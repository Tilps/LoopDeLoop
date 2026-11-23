<?php
declare(strict_types=1);

// Hardened LoopDeLoop Debug Telemetry Collector
// - Rejects any missing/mismatched X-LoopDeLoop-Nonce BEFORE reading request body or touching disk.
// - Stores strictly validated JSON outside the web root with atomic renames and disk/rate quotas.

ini_set('display_errors', '0');
error_reporting(0);

header('Content-Type: application/json; charset=utf-8');
header('X-Content-Type-Options: nosniff');
header('Cache-Control: no-store, no-cache, must-revalidate');
header('Referrer-Policy: no-referrer');

const NONCE_FILE = '/etc/loopdeloop-telemetry/nonce';
const DATA_ROOT = '/var/lib/loopdeloop-telemetry';
const SUBMISSIONS_DIR = DATA_ROOT . '/submissions';
const RATELIMIT_DIR = DATA_ROOT . '/ratelimit';
const QUOTA_FILE = DATA_ROOT . '/quota.json';

const MAX_BODY_BYTES = 262144;            // 256 KiB per submission
const MAX_JSON_DEPTH = 16;                // Prevent deep-nesting attacks
const MAX_EVENTS = 5000;                  // Max events per solve
const MAX_NOTE_CHARS = 500;               // Max UTF-8 chars in optional note
const MAX_STORED_FILES = 10000;           // Hard file count quota
const MAX_STORED_BYTES = 262144000;       // 250 MiB total storage quota
const MIN_FREE_DISK_BYTES = 536870912;    // Require at least 512 MiB free on partition
const RATE_LIMIT_WINDOW_SEC = 600;        // 10-minute sliding window
const RATE_LIMIT_MAX_UPLOADS = 30;        // Max uploads per IP per window

function respond(int $status, array $body): never
{
    http_response_code($status);
    echo json_encode($body, JSON_UNESCAPED_SLASHES | JSON_UNESCAPED_UNICODE);
    exit;
}

// 1. Strict HTTP method check
if (($_SERVER['REQUEST_METHOD'] ?? '') !== 'POST') {
    header('Allow: POST');
    respond(405, ['ok' => false, 'message' => 'Method not allowed.']);
}

// 2. IMMEDIATE constant-time shared nonce verification BEFORE reading body or doing any processing
$serverNonceRaw = @file_get_contents(NONCE_FILE);
if (!is_string($serverNonceRaw)) {
    respond(503, ['ok' => false, 'message' => 'Telemetry receiver is not configured.']);
}
$serverNonce = trim($serverNonceRaw);
if (!preg_match('/^[A-Za-z0-9_-]{32,128}$/', $serverNonce)) {
    respond(503, ['ok' => false, 'message' => 'Telemetry receiver nonce is invalid.']);
}

$clientNonce = $_SERVER['HTTP_X_LOOPDELOOP_NONCE'] ?? '';
if (!is_string($clientNonce) || !preg_match('/^[A-Za-z0-9_-]{32,128}$/', $clientNonce) || !hash_equals($serverNonce, $clientNonce)) {
    // Discard immediately without reading php://input or processing anything.
    respond(403, ['ok' => false, 'message' => 'Forbidden.']);
}

// 3. Content-Type & Content-Length pre-checks
$contentType = $_SERVER['CONTENT_TYPE'] ?? '';
if (!is_string($contentType) || stripos($contentType, 'application/json') !== 0) {
    respond(415, ['ok' => false, 'message' => 'Expected application/json.']);
}

$contentLength = $_SERVER['CONTENT_LENGTH'] ?? null;
if ($contentLength !== null && (!ctype_digit((string)$contentLength) || (int)$contentLength > MAX_BODY_BYTES)) {
    respond(413, ['ok' => false, 'message' => 'Payload too large.']);
}

// 4. Per-IP sliding-window rate limit (using non-spoofable REMOTE_ADDR only)
$remoteIp = $_SERVER['REMOTE_ADDR'] ?? 'unknown';
if (is_dir(RATELIMIT_DIR) && is_writable(RATELIMIT_DIR)) {
    $ipHash = hash('sha256', $remoteIp);
    $rlFile = RATELIMIT_DIR . '/' . $ipHash . '.json';
    $fp = @fopen($rlFile, 'c+');
    if ($fp !== false) {
        if (flock($fp, LOCK_EX)) {
            $now = time();
            $cutoff = $now - RATE_LIMIT_WINDOW_SEC;
            $rawRl = stream_get_contents($fp);
            $timestamps = [];
            if (is_string($rawRl) && $rawRl !== '') {
                $decodedRl = json_decode($rawRl, true);
                if (is_array($decodedRl)) {
                    foreach ($decodedRl as $ts) {
                        if (is_int($ts) && $ts >= $cutoff && $ts <= $now + 60) {
                            $timestamps[] = $ts;
                        }
                    }
                }
            }
            if (count($timestamps) >= RATE_LIMIT_MAX_UPLOADS) {
                flock($fp, LOCK_UN);
                fclose($fp);
                header('Retry-After: ' . RATE_LIMIT_WINDOW_SEC);
                respond(429, ['ok' => false, 'message' => 'Too many uploads. Please wait a few minutes.']);
            }
            $timestamps[] = $now;
            ftruncate($fp, 0);
            rewind($fp);
            fwrite($fp, json_encode($timestamps));
            fflush($fp);
            flock($fp, LOCK_UN);
        }
        fclose($fp);
    }
}

// 5. Bounded body read
$rawBody = file_get_contents('php://input', false, null, 0, MAX_BODY_BYTES + 1);
if (!is_string($rawBody) || $rawBody === '') {
    respond(400, ['ok' => false, 'message' => 'Empty request body.']);
}
if (strlen($rawBody) > MAX_BODY_BYTES) {
    respond(413, ['ok' => false, 'message' => 'Payload too large.']);
}

// 6. Strict JSON parse with depth cap
try {
    $data = json_decode($rawBody, true, MAX_JSON_DEPTH, JSON_THROW_ON_ERROR);
} catch (Throwable) {
    respond(400, ['ok' => false, 'message' => 'Malformed JSON payload.']);
}

if (!is_array($data) || array_is_list($data)) {
    respond(400, ['ok' => false, 'message' => 'Expected JSON object.']);
}

// 7. Strict schema validation & whitelist reconstruction
function cleanStr(mixed $v, int $maxLen, bool $allowNewlines = false): ?string
{
    if (!is_string($v)) {
        return null;
    }
    $s = str_replace("\0", '', $v);
    if (!$allowNewlines) {
        $s = str_replace(["\r", "\n"], ' ', $s);
    }
    $s = trim($s);
    if ($s === '') {
        return null;
    }
    if (function_exists('mb_substr')) {
        return mb_substr($s, 0, $maxLen, 'UTF-8');
    }
    return substr($s, 0, $maxLen);
}

function boundedInt(mixed $v, int $min, int $max, int $default = 0): int
{
    if (!is_int($v)) {
        return $default;
    }
    return max($min, min($max, $v));
}

$version = $data['Version'] ?? null;
if ($version !== 1) {
    respond(400, ['ok' => false, 'message' => 'Unsupported payload version.']);
}

$puzzleCode = cleanStr($data['PuzzleCode'] ?? null, 2048);
if ($puzzleCode === null || !preg_match('/^[\x20-\x7E]{1,2048}$/', $puzzleCode)) {
    respond(400, ['ok' => false, 'message' => 'Missing or invalid PuzzleCode.']);
}

$meshType = cleanStr($data['MeshType'] ?? null, 64);
if ($meshType === null || !preg_match('/^[A-Za-z0-9_-]{1,64}$/', $meshType)) {
    respond(400, ['ok' => false, 'message' => 'Missing or invalid MeshType.']);
}

$installationId = '';
if (isset($data['InstallationId']) && is_string($data['InstallationId'])) {
    $cand = trim($data['InstallationId']);
    if ($cand !== '' && preg_match('/^[0-9a-fA-F]{8}-[0-9a-fA-F]{4}-[0-9a-fA-F]{4}-[0-9a-fA-F]{4}-[0-9a-fA-F]{12}$/', $cand)) {
        $installationId = strtolower($cand);
    }
}

$difficultyRating = null;
if (isset($data['DifficultyRating']) && is_int($data['DifficultyRating']) && $data['DifficultyRating'] >= 1 && $data['DifficultyRating'] <= 5) {
    $difficultyRating = $data['DifficultyRating'];
}

$qualityRating = null;
if (isset($data['QualityRating']) && is_int($data['QualityRating']) && $data['QualityRating'] >= 1 && $data['QualityRating'] <= 5) {
    $qualityRating = $data['QualityRating'];
}

$note = cleanStr($data['Note'] ?? null, MAX_NOTE_CHARS, allowNewlines: true);

$client = null;
if (isset($data['Client']) && is_array($data['Client'])) {
    $c = $data['Client'];
    $dpr = isset($c['DevicePixelRatio']) && (is_int($c['DevicePixelRatio']) || is_float($c['DevicePixelRatio']))
        ? round(max(0.1, min(16.0, (float)$c['DevicePixelRatio'])), 2)
        : 1.0;
    $client = [
        'ScreenWidth' => boundedInt($c['ScreenWidth'] ?? 0, 0, 32768),
        'ScreenHeight' => boundedInt($c['ScreenHeight'] ?? 0, 0, 32768),
        'ViewportWidth' => boundedInt($c['ViewportWidth'] ?? 0, 0, 32768),
        'ViewportHeight' => boundedInt($c['ViewportHeight'] ?? 0, 0, 32768),
        'DevicePixelRatio' => $dpr,
        'SawTouch' => (bool)($c['SawTouch'] ?? false),
        'SawMouse' => (bool)($c['SawMouse'] ?? false),
        'SawPen' => (bool)($c['SawPen'] ?? false),
        'SawKeyboard' => (bool)($c['SawKeyboard'] ?? false),
    ];
}

$settings = [];
if (isset($data['Settings']) && is_array($data['Settings'])) {
    $s = $data['Settings'];
    $settings = [
        'AutoMoveLevel' => boundedInt($s['AutoMoveLevel'] ?? 0, 0, 4),
        'DisallowFalseMove' => (bool)($s['DisallowFalseMove'] ?? false),
        'ShowCellColors' => (bool)($s['ShowCellColors'] ?? false),
        'ShowEdgeColors' => (bool)($s['ShowEdgeColors'] ?? false),
        'ShowHintHeatMap' => (bool)($s['ShowHintHeatMap'] ?? false),
        'AutoStart' => (bool)($s['AutoStart'] ?? false),
    ];
}

$rawEvents = $data['Events'] ?? null;
if (!is_array($rawEvents) || count($rawEvents) > MAX_EVENTS) {
    respond(400, ['ok' => false, 'message' => 'Invalid or excessive Events list.']);
}

$events = [];
foreach ($rawEvents as $ev) {
    if (!is_array($ev)) {
        respond(400, ['ok' => false, 'message' => 'Invalid event entry.']);
    }
    $kind = cleanStr($ev['Kind'] ?? null, 32);
    if ($kind === null || !preg_match('/^[a-z0-9_]{1,32}$/', $kind)) {
        respond(400, ['ok' => false, 'message' => 'Invalid event Kind.']);
    }
    $cleanEv = [
        'Seq' => boundedInt($ev['Seq'] ?? 0, 0, MAX_EVENTS),
        'ElapsedMs' => boundedInt($ev['ElapsedMs'] ?? 0, 0, 864000000),
        'DeltaMs' => boundedInt($ev['DeltaMs'] ?? 0, 0, 864000000),
        'Kind' => $kind,
    ];
    if (isset($ev['Index']) && is_int($ev['Index'])) {
        $cleanEv['Index'] = boundedInt($ev['Index'], -1, 100000);
    }
    if (isset($ev['SecondaryIndex']) && is_int($ev['SecondaryIndex'])) {
        $cleanEv['SecondaryIndex'] = boundedInt($ev['SecondaryIndex'], -1, 100000);
    }
    if (isset($ev['FromState'])) {
        $fs = cleanStr($ev['FromState'], 32);
        if ($fs !== null) $cleanEv['FromState'] = $fs;
    }
    if (isset($ev['ToState'])) {
        $ts = cleanStr($ev['ToState'], 32);
        if ($ts !== null) $cleanEv['ToState'] = $ts;
    }
    if (isset($ev['IsAlternative']) && is_bool($ev['IsAlternative'])) {
        $cleanEv['IsAlternative'] = $ev['IsAlternative'];
    }
    if (isset($ev['Succeeded']) && is_bool($ev['Succeeded'])) {
        $cleanEv['Succeeded'] = $ev['Succeeded'];
    }
    if (isset($ev['Detail'])) {
        $dt = cleanStr($ev['Detail'], 64);
        if ($dt !== null) $cleanEv['Detail'] = $dt;
    }
    $events[] = $cleanEv;
}

// 8. O(1) disk free space & storage quota check (reconciles via scan only if quota.json is missing or hits cap)
if (!is_dir(SUBMISSIONS_DIR) || !is_writable(SUBMISSIONS_DIR)) {
    respond(503, ['ok' => false, 'message' => 'Telemetry storage is unavailable.']);
}

$freeBytes = @disk_free_space(SUBMISSIONS_DIR);
if (is_float($freeBytes) && $freeBytes < MIN_FREE_DISK_BYTES) {
    respond(507, ['ok' => false, 'message' => 'Server disk space threshold reached.']);
}

function scanSubmissionsQuota(): array
{
    $count = 0;
    $bytes = 0;
    $dh = @opendir(SUBMISSIONS_DIR);
    if ($dh !== false) {
        while (($entry = readdir($dh)) !== false) {
            if ($entry === '.' || $entry === '..' || str_starts_with($entry, '.')) {
                continue;
            }
            $count++;
            $sz = @filesize(SUBMISSIONS_DIR . '/' . $entry);
            if (is_int($sz)) {
                $bytes += $sz;
            }
        }
        closedir($dh);
    }
    return ['count' => $count, 'bytes' => $bytes, 'scannedAt' => time()];
}

// 9. Build normalized record & write atomically using server-generated filename only
$submissionId = gmdate('Ymd_His') . '_' . bin2hex(random_bytes(6));
$record = [
    'SubmissionId' => $submissionId,
    'ReceivedAtUtc' => gmdate('Y-m-d\TH:i:s\Z'),
    'Version' => 1,
    'InstallationId' => $installationId,
    'PuzzleCode' => $puzzleCode,
    'MeshType' => $meshType,
    'Width' => boundedInt($data['Width'] ?? 0, 0, 200),
    'Height' => boundedInt($data['Height'] ?? 0, 0, 200),
    'Difficulty' => boundedInt($data['Difficulty'] ?? 0, 0, 10),
    'EdgeCount' => boundedInt($data['EdgeCount'] ?? 0, 0, 100000),
    'CellCount' => boundedInt($data['CellCount'] ?? 0, 0, 100000),
    'StartedAtUtc' => cleanStr($data['StartedAtUtc'] ?? null, 64),
    'CompletedAtUtc' => cleanStr($data['CompletedAtUtc'] ?? null, 64),
    'TotalElapsedMs' => boundedInt($data['TotalElapsedMs'] ?? 0, 0, 864000000),
    'IsSolved' => (bool)($data['IsSolved'] ?? false),
    'WasAutoSolved' => (bool)($data['WasAutoSolved'] ?? false),
    'DifficultyRating' => $difficultyRating,
    'QualityRating' => $qualityRating,
    'Note' => $note,
    'Client' => $client,
    'Settings' => $settings,
    'Events' => $events,
];

$encoded = json_encode($record, JSON_UNESCAPED_SLASHES | JSON_UNESCAPED_UNICODE);
if (!is_string($encoded)) {
    respond(500, ['ok' => false, 'message' => 'Failed to encode submission.']);
}
$payloadLine = $encoded . "\n";
$payloadBytes = strlen($payloadLine);

// Reserve quota atomically in O(1) using flocked quota.json
$qFp = @fopen(QUOTA_FILE, 'c+');
if ($qFp === false || !flock($qFp, LOCK_EX)) {
    if ($qFp !== false) {
        fclose($qFp);
    }
    respond(503, ['ok' => false, 'message' => 'Telemetry quota tracker is unavailable.']);
}

$quota = null;
$rawQuota = stream_get_contents($qFp);
if (is_string($rawQuota) && $rawQuota !== '') {
    $decodedQuota = json_decode($rawQuota, true);
    if (is_array($decodedQuota) && isset($decodedQuota['count'], $decodedQuota['bytes'])
        && is_int($decodedQuota['count']) && is_int($decodedQuota['bytes'])) {
        $quota = [
            'count' => $decodedQuota['count'],
            'bytes' => $decodedQuota['bytes'],
            'scannedAt' => is_int($decodedQuota['scannedAt'] ?? null) ? $decodedQuota['scannedAt'] : 0,
        ];
    }
}

// Reconcile only on cold start (missing/corrupt quota.json) or when cached count hits cap
// (throttled to at most once per 60s to prevent scan amplification while full).
$atCap = $quota !== null && ($quota['count'] >= MAX_STORED_FILES || ($quota['bytes'] + $payloadBytes) > MAX_STORED_BYTES);
if ($quota === null || ($atCap && (time() - $quota['scannedAt']) >= 60)) {
    $quota = scanSubmissionsQuota();
}

if ($quota['count'] >= MAX_STORED_FILES || ($quota['bytes'] + $payloadBytes) > MAX_STORED_BYTES) {
    ftruncate($qFp, 0);
    rewind($qFp);
    fwrite($qFp, json_encode($quota));
    fflush($qFp);
    flock($qFp, LOCK_UN);
    fclose($qFp);
    respond(507, ['ok' => false, 'message' => 'Telemetry storage quota reached.']);
}

$tmpPath = SUBMISSIONS_DIR . '/.' . $submissionId . '.tmp';
$finalPath = SUBMISSIONS_DIR . '/' . $submissionId . '.json';

if (@file_put_contents($tmpPath, $payloadLine, LOCK_EX) === false) {
    @unlink($tmpPath);
    flock($qFp, LOCK_UN);
    fclose($qFp);
    respond(500, ['ok' => false, 'message' => 'Failed to persist submission.']);
}
@chmod($tmpPath, 0640);
if (!@rename($tmpPath, $finalPath)) {
    @unlink($tmpPath);
    flock($qFp, LOCK_UN);
    fclose($qFp);
    respond(500, ['ok' => false, 'message' => 'Failed to finalize submission.']);
}

$quota['count']++;
$quota['bytes'] += $payloadBytes;
ftruncate($qFp, 0);
rewind($qFp);
fwrite($qFp, json_encode($quota));
fflush($qFp);
flock($qFp, LOCK_UN);
fclose($qFp);

respond(201, [
    'ok' => true,
    'id' => $submissionId,
    'message' => 'Debug log saved.'
]);
