# Pathfinding Greybox Matrix Testing Setup Instructions

## Overview
This document outlines the step-by-step process for setting up a scene to perform automated greybox matrix testing of the pathfinding algorithms.

## Scene Setup Steps

1. Create a new scene or use an existing pathfinding scene

2. Add the required components to the scene:
   - GridMap
   - NPC
   - UI Canvas for the test controls

3. Create the following UI elements on the Canvas:
   - Panel (as a container)
   - Text (TMP) for status display
   - Button for starting tests
   - Slider for progress visualization

4. Create an empty GameObject and attach the PathfindingTester.cs script

5. Configure the PathfindingTester component:
   - Assign the GridMap reference
   - Assign the NPC reference
   - Assign the UI components (button, text, progress bar)
   - Configure test parameters as needed:
     - Tests Per Combination
     - Delay Between Tests
     - Results File Name

## UI Layout Recommendations

1. Place the status text at the top of the panel to show current test status
2. Place the progress bar below the status text
3. Place the "Start Tests" button at the bottom
4. Make the panel semi-transparent so you can still see the grid behind it

## Test Matrix Configuration

You can customize the test matrix by modifying these arrays in the PathfindingTester.cs script:

- algorithmsToTest: Which algorithms to test
- gridSizesToTest: Which grid sizes to test
- mazeDensitiesToTest: Which maze densities to test
- diagonalMovementOptions: Whether to test with diagonals enabled/disabled

## Running Tests

1. Enter Play mode in the Unity editor
2. Click the "Start Tests" button
3. Wait for all tests to complete
4. Test results will be saved in a CSV file in the "TestResults" folder in the persistent data path

## Analyzing Results

The CSV file contains detailed information about each test:
- Algorithm used
- Grid size
- Maze density
- Diagonal movement setting
- Time taken (ms)
- Path length (nodes)
- Nodes explored
- Memory used
- Whether a path was found
- Test index (for repeated tests)

Import the CSV into a spreadsheet application for further analysis and visualization. 