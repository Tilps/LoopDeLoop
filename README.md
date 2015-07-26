# Loop-de-loop

Loop-de-loop is a game for puzzles on a grid where numbers in grid cells indicate the number of filled in edges
of that cell, and all filled in edges must form a single non-crossing loop.

The core solving/generating code is shared by 2 frontends.  One full featured windows app which includes a
competitive multiplayer mode, and a more simple silverlight component for hosting on the web.

The solver has a 'lot' of options, which in generating mode can be useful for controlling desired difficulty.

The core code is rather more complex than might be desired, because it is written to work with arbitrary grids
not just square grids.  It is probably not bug free, but it has been manually tested quite a lot.