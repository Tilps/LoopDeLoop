using LoopDeLoop;
using LoopDeLoop.TrialOrdering;

namespace LoopDeLoop.Tests;

[TestClass]
[DoNotParallelize]
public sealed class NeuralTrialOrderingTests
{
    [TestMethod]
    public void DefaultEmbeddedModel_LoadsWithExpectedMetadata()
    {
        IncidenceGnnModel model = IncidenceGnnModel.Default;
        Assert.AreEqual(2, model.Version);
        Assert.AreEqual(GraphFeatureExtractor.EdgeFeatDim, model.Fe);
        Assert.AreEqual(GraphFeatureExtractor.CellFeatDim, model.Fc);
        Assert.AreEqual(GraphFeatureExtractor.VertFeatDim, model.Fv);
        Assert.AreEqual(GraphFeatureExtractor.GlobalFeatDim, model.Fg);
        Assert.AreEqual(16, model.Hidden);
        Assert.AreEqual(2, model.NumLayers);
        Assert.IsTrue(model.UseColorPool);
        Assert.IsTrue(model.UseMaxPool);
    }

    [TestMethod]
    public void MakeMesh_AttachesPolicyOnlyForDifficulties2To4()
    {
        bool prev = PuzzleHelper.UseNeuralTrialOrdering;
        try
        {
            PuzzleHelper.UseNeuralTrialOrdering = true;
            for (int d = 0; d <= 5; d++)
            {
                Mesh mesh = PuzzleHelper.MakeMesh(8, 8, MeshType.Square, d);
                bool expected = d >= 2 && d <= 4;
                Assert.AreEqual(expected, mesh.TrialOrderingPolicy != null, $"Difficulty {d}");
            }

            PuzzleHelper.UseNeuralTrialOrdering = false;
            Mesh disabled = PuzzleHelper.MakeMesh(8, 8, MeshType.Square, 4);
            Assert.IsNull(disabled.TrialOrderingPolicy);
        }
        finally
        {
            PuzzleHelper.UseNeuralTrialOrdering = prev;
        }
    }

    [TestMethod]
    [DataRow(MeshType.Square, 10, 10, 4, 101)]
    [DataRow(MeshType.CairoPentagons, 5, 5, 4, 202)]
    [DataRow(MeshType.Hexagonal, 5, 8, 3, 303)]
    [DataRow(MeshType.Triangle, 6, 6, 3, 404)]
    public void NeuralOrdering_MatchesSequentialSolveAndAnalysis(MeshType type, int w, int h, int diff, int seed)
    {
        bool prev = PuzzleHelper.UseNeuralTrialOrdering;
        try
        {
            PuzzleHelper.UseNeuralTrialOrdering = false;
            Mesh puzzle = PuzzleHelper.MakeMesh(w, h, type, diff);
            puzzle.Generate(new Random(seed));
            puzzle.Clear();

            PuzzleAnalysisResult seqAnalysis = PuzzleHelper.AnalyzePuzzle(puzzle);
            Mesh seqSolver = PuzzleHelper.CreateFullSolverMesh(puzzle);
            seqSolver.Clear();
            SolveState seqState = seqSolver.TrySolve(noRollback: true);

            PuzzleHelper.UseNeuralTrialOrdering = true;
            PuzzleAnalysisResult nnAnalysis = PuzzleHelper.AnalyzePuzzle(puzzle);
            Mesh nnSolver = PuzzleHelper.CreateFullSolverMesh(puzzle);
            nnSolver.Clear();
            SolveState nnState = nnSolver.TrySolve(noRollback: true);

            Assert.AreEqual(SolveState.Solved, seqState);
            Assert.AreEqual(SolveState.Solved, nnState);
            Assert.IsTrue(PuzzleHelper.CheckIsSolved(nnSolver.SolutionFound ?? nnSolver));
            Assert.AreEqual(seqAnalysis.SolveStatus, nnAnalysis.SolveStatus);
            Assert.AreEqual(seqAnalysis.TechniqueTier, nnAnalysis.TechniqueTier);
            Assert.AreEqual(seqAnalysis.LookaheadDepth, nnAnalysis.LookaheadDepth);
        }
        finally
        {
            PuzzleHelper.UseNeuralTrialOrdering = prev;
        }
    }
}
