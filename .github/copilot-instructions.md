# ScrapLine Unity Game Development Instructions

**Always reference these instructions first and fallback to search or bash commands only when you encounter unexpected information that does not match the info here.**

**Always keep these instructions up to date with changes that are made or new information that is found while implementing changes that will make things easier for future you.**

**Always read the readme and keep it up to date with any changes that you make OR any inconcistencies you find with the codebase.**

ScrapLine is a Unity 6000.2.3f1 (Unity 6 LTS) mobile-first factory automation game where players build conveyor belt systems to process scrap materials. The game features a touch-optimized grid-based building system with machines (shredders, spawners, sellers), conveyors, and item processing mechanics. Players can place items like aluminum cans and process them into shredded aluminum using various machines in a factory layout, all optimized for mobile devices.

## CRITICAL: ScriptSandbox Development Workflow

**ALL script changes are incomplete until ScriptSandbox compiles successfully.**

### ScriptSandbox Project Structure
The repository now contains two main projects:
```
/home/runner/work/ScrapLine/
├── ScriptSandbox/          # C# compilation sandbox (REQUIRED)
│   ├── Unity/              # Mock Unity classes
│   ├── Scripts/            # All Unity scripts copied here
│   ├── Tests/              # Unit tests for script logic
│   └── ScriptSandbox.csproj
└── ScrapLine/             # Unity project
    └── Assets/Scripts/    # Original Unity scripts
```

### Mandatory Script Development Process

1. **All script changes MUST be validated in ScriptSandbox first**
2. **No script modifications are complete without ScriptSandbox compilation**
3. **When modifying any Unity script:**
   - Make changes in both `ScrapLine/Assets/Scripts/` AND `ScriptSandbox/Scripts/`
   - Compile ScriptSandbox: `cd /home/runner/work/ScrapLine/ScriptSandbox && dotnet build`
   - Add/update unit tests in `ScriptSandbox/Tests/`
   - Ensure ScriptSandbox compiles without errors before proceeding

### ScriptSandbox Commands

**Build and validate scripts:**
```bash
cd /home/runner/work/ScrapLine/ScriptSandbox
dotnet build --verbosity minimal
```

**Run unit tests:**
```bash
cd /home/runner/work/ScrapLine/ScriptSandbox
dotnet test --verbosity minimal
```

**Update scripts after Unity changes:**
```bash
cd /home/runner/work/ScrapLine/ScriptSandbox
cp -r ../ScrapLine/Assets/Scripts/* Scripts/
dotnet build
```

### When to Update ScriptSandbox

- **ALWAYS** after modifying any C# script in Unity
- When adding new Unity scripts
- When changing script dependencies or imports
- Before committing any script changes
- When encountering compilation errors in Unity

### ScriptSandbox Mock Classes

The ScriptSandbox contains comprehensive mock implementations of:
- UnityEngine core classes (MonoBehaviour, GameObject, Transform, etc.)
- Unity UI classes (Button, Image, Text, Canvas, etc.)  
- TextMeshPro classes (TextMeshProUGUI, TMP_Text, etc.)
- Unity utilities (Resources, Application, PlayerPrefs, etc.)

If you encounter missing Unity classes during compilation:
1. Add the missing class to the appropriate mock file in `ScriptSandbox/Unity/`
2. Ensure the mock provides basic functionality needed by the scripts
3. Test compilation after adding mocks

### Required Testing in ScriptSandbox

When changing scripts, MUST add basic unit tests covering:
- Class instantiation works
- Basic method calls don't throw exceptions
- Core business logic functions correctly
- Data model serialization/deserialization

**Example test structure:**
```csharp
[Test]
public void NewFeature_BasicFunctionality()
{
    // Arrange
    var testData = new TestDataClass();
    
    // Act & Assert
    Assert.DoesNotThrow(() => testData.NewMethod());
    Assert.IsNotNull(testData.Result);
}
```

## Working Effectively

### Primary Development: ScriptSandbox Validation
**ALWAYS use ScriptSandbox for script development and validation before Unity testing.**

ScriptSandbox provides:
- Fast compilation validation (seconds vs minutes)
- Unit testing capability outside Unity
- Continuous integration friendly
- No Unity installation required for basic script validation

### Secondary: Unity Installation and Setup (When Required)
**Only needed for full Unity builds and scene testing. Use ScriptSandbox for daily script development.**

**WARNING: Unity installation and builds can take 45+ minutes. NEVER CANCEL long-running operations.**

- **Install Unity Hub:**
  ```bash
  # Download Unity Hub (requires internet access)
  wget https://public-cdn.cloud.unity3d.com/hub/prod/UnityHub.AppImage -O ~/UnityHub.AppImage
  chmod +x ~/UnityHub.AppImage
  ```

- **Install Unity 6000.2.3f1 (Required Version):**
  ```bash
  # Unity Hub installation takes 20-30 minutes. NEVER CANCEL. Set timeout to 45+ minutes.
  ~/UnityHub.AppImage --headless install --version 6000.2.3f1 --changeset c7638eb16d91
  # Alternative: Install via Unity Hub GUI if available
  # Unity Hub > Installs > Add > Unity 6000.2.3f1
  ```

- **Project Dependencies Installation:**
  ```bash
  # Unity automatically resolves package dependencies on project open
  # This process takes 5-15 minutes. NEVER CANCEL. Set timeout to 30+ minutes.
  ~/UnityHub.AppImage --headless --projectPath /path/to/ScrapLine
  ```

### Building the Project (CRITICAL TIMING)
**WARNING: Unity builds take 10-45 minutes depending on platform. ALWAYS wait for completion.**

- **Open Project in Unity:**
  ```bash
  # Opening Unity project takes 5-10 minutes for first-time setup. NEVER CANCEL.
  ~/UnityHub.AppImage --headless --projectPath /home/runner/work/ScrapLine/ScrapLine
  ```

- **Build for Development (Standalone):**
  ```bash
  # Unity command-line build takes 15-30 minutes. NEVER CANCEL. Set timeout to 60+ minutes.
  Unity -batchmode -quit -projectPath /home/runner/work/ScrapLine/ScrapLine -buildTarget StandaloneLinux64 -buildPath ./Builds/Linux/ -executeMethod BuildScript.BuildLinux -logFile build.log
  ```

- **Build for WebGL (if supported):**
  ```bash
  # WebGL builds take 30-45 minutes. NEVER CANCEL. Set timeout to 90+ minutes.
  Unity -batchmode -quit -projectPath /home/runner/work/ScrapLine/ScrapLine -buildTarget WebGL -buildPath ./Builds/WebGL/ -executeMethod BuildScript.BuildWebGL -logFile webgl_build.log
  ```

### Testing and Validation (MANDATORY)
**ALWAYS run these validation steps after making changes. Each step is critical.**

- **Play Mode Testing:**
  ```bash
  # Unity Test Runner takes 5-15 minutes. NEVER CANCEL. Set timeout to 30+ minutes.
  Unity -batchmode -quit -projectPath /home/runner/work/ScrapLine/ScrapLine -runTests -testPlatform PlayMode -testResults TestResults.xml -logFile test.log
  ```

- **Manual Validation Scenarios (REQUIRED):**
  1. **Grid System Test:** Place a conveyor belt, verify it appears on grid
  2. **Item Processing Test:** Spawn an aluminum can, watch it move through conveyor
  3. **Machine Interaction Test:** Place a shredder machine, process can into shredded aluminum
  4. **UI Responsiveness Test:** Test both desktop and mobile scenes
  5. **Save/Load Test:** Save game state, reload, verify persistence

### Running the Game
- **Play in Editor:**
  ```bash
  # Opens Unity Editor in play mode. Takes 2-5 minutes to start.
  Unity -projectPath /home/runner/work/ScrapLine/ScrapLine
  # Manual: Open MobileGridScene.unity, press Play button
  ```

- **Standalone Build:**
  ```bash
  # Run the built executable
  ./Builds/Linux/ScrapLine.x86_64
  ```

### Alternative Validation (.NET Available)
Since .NET 8.0.119 is available, you can validate basic compilation without Unity:

```bash
# Create a simple C# project to test compilation. Takes 1-2 minutes.
cd /tmp && dotnet new console -o unity-compile-test --force
# Test basic .NET compilation (validated working)
cd unity-compile-test && dotnet build --verbosity minimal
```

### JSON Resource Validation (Always Available)
Validate game data files for syntax errors:

```bash
# Validate JSON syntax (takes seconds, always works)
python3 -m json.tool /home/runner/work/ScrapLine/ScrapLine/Assets/Resources/items.json > /dev/null && echo "items.json valid" || echo "items.json invalid"
python3 -m json.tool /home/runner/work/ScrapLine/ScrapLine/Assets/Resources/machines.json > /dev/null && echo "machines.json valid" || echo "machines.json invalid"  
python3 -m json.tool /home/runner/work/ScrapLine/ScrapLine/Assets/Resources/recipes.json > /dev/null && echo "recipes.json valid" || echo "recipes.json invalid"
```

### ScriptSandbox Validation (Primary Method)
**PREFERRED method for script validation - use this first.**

```bash
# Comprehensive script validation (takes 5-15 seconds)
cd /home/runner/work/ScrapLine/ScriptSandbox
dotnet build --verbosity minimal

# Run unit tests for logic validation
dotnet test --verbosity minimal

# Update scripts from Unity project
cp -r ../ScrapLine/Assets/Scripts/* Scripts/
dotnet build
```

### Legacy Validation Methods (Deprecated)
~~Limited Environment Workarounds are replaced by ScriptSandbox which provides comprehensive validation without Unity installation.~~

**ScriptSandbox replaces these old methods:**
- ~~Documentation Review~~ → ScriptSandbox provides actual compilation validation
- ~~Syntax Validation with basic .NET~~ → ScriptSandbox validates actual Unity scripts  
- ~~Manual Logic Review~~ → ScriptSandbox supports unit testing of logic
- ~~Configuration Validation~~ → ScriptSandbox validates script interdependencies

**Still valid:**
- JSON Validation for resource files (see above)

**IMPORTANT:** ScriptSandbox works in all environments and provides superior validation to manual methods.

## Game-Specific Knowledge

### Core Game Mechanics
- **Items:** Aluminum cans spawn and get processed into shredded aluminum
- **Machines:**
  - **Spawner:** Generates items (max 1-3 based on upgrades), placed on bottom edge
  - **Conveyor:** Moves items in grid, can be placed anywhere
  - **Shredder:** Processes cans → shredded aluminum, upgradeable speed
  - **Seller:** Removes items from game, placed on top edge
- **Grid Layout:** Specific placement rules per machine type
- **Upgrades:** Machines can be upgraded for faster processing times
- **Movement:** Items move smoothly between grid cells with configurable speed

### Key Scripts to Understand
- **GameManager.cs:** Core game loop, spawning, save/load, timing
- **UIGridManager.cs:** Grid display, cell management, visual updates  
- **UICell.cs:** Individual grid cell behavior and item placement
- **ConveyorBelt.cs:** Conveyor belt logic and item movement
- **MachineButton.cs:** UI for machine selection and placement

### Resource Files (Critical for Game Data)
- **items.json:** Defines can, shreddedAluminum with display names and sprites
- **machines.json:** Defines all machine types, processing times, upgrade costs
- **recipes.json:** Defines transformation rules (can → shreddedAluminum)

### Project Scale and Complexity
- **Unity Assets:** 25 total (.asset, .prefab, .unity files)
- **C# Scripts:** 14 total scripts
- **Scenes:** 1 (MobileGridScene) - primary gameplay scene optimized for mobile
- **Resource Files:** 3 JSON data files
- **Project Type:** Small-to-medium Unity project, manageable scope

This is a focused project with clear boundaries, making it ideal for rapid development and testing.
```
Assets/
├── Scenes/                          # Unity scenes
│   └── MobileGridScene.unity       # Main mobile gameplay scene
├── Scripts/                        # C# game logic
│   ├── Game/GameManager.cs         # Core game management
│   ├── Grid/UIGridManager.cs       # Grid system management
│   ├── Machines/                   # Machine-related scripts
│   ├── Conveyors/                  # Conveyor belt systems
│   ├── Backend/Data/               # Data models and persistence
│   └── UI/                         # User interface scripts
├── Resources/                      # Loadable game data
│   ├── items.json                  # Item definitions (cans, materials)
│   ├── machines.json               # Machine configurations
│   └── recipes.json                # Processing recipes
├── Prefabs/                        # Reusable game objects
├── Materials/                      # Unity materials and shaders
└── Sprites/                        # 2D graphics assets
```

### Important Configuration Files
- `ProjectSettings/ProjectVersion.txt` - Unity version requirement
- `Packages/manifest.json` - Package dependencies  
- `ProjectSettings/EditorBuildSettings.asset` - Build configuration

## Validation Requirements

### Pre-Commit Checklist (MANDATORY)
Before committing any changes, ALWAYS complete these steps:

1. **Code Compilation Check:**
   ```bash
   # Verify no compilation errors. Takes 1-2 minutes.
   Unity -batchmode -quit -projectPath /home/runner/work/ScrapLine/ScrapLine -executeMethod CompileCheck -logFile compile.log
   ```

2. **Play Mode Testing:** 
   - Open MobileGridScene.unity
   - Enter Play Mode
   - Test basic conveyor and machine functionality with touch controls

3. **Build Verification:**
   ```bash
   # Test build process. Takes 15-30 minutes. NEVER CANCEL.
   Unity -batchmode -quit -projectPath /home/runner/work/ScrapLine/ScrapLine -buildTarget StandaloneLinux64 -buildPath ./TestBuild/ -logFile test_build.log
   ```

### Critical Testing Scenarios
After any code changes, ALWAYS test these scenarios:

1. **Item Spawning and Movement:**
   - Open MobileGridScene.unity in Unity Editor
   - Place conveyor belts in a line on the grid using touch controls
   - Use spawner machine to generate aluminum cans
   - Verify smooth movement along conveyor path at correct speed

2. **Machine Processing Chain:**
   - Place spawner machine → conveyor → shredder machine → conveyor → seller machine
   - Configure spawner to spawn "can" items
   - Verify cans move to shredder and get converted to "shreddedAluminum"
   - Verify processed items move to seller and disappear

3. **Grid System Integrity:**
   - Test grid cell placement and removal of machines using touch controls
   - Verify UI updates correctly when machines are placed/removed
   - Check cell highlighting and selection feedback for mobile interface
   - Test different machine types: conveyor, spawner, shredder, seller

4. **Save/Load Functionality:**
   - Build a factory setup with multiple machines and conveyors
   - Save game using GameManager's JSON serialization
   - Close and reopen Unity/game
   - Load save file and verify all machines, conveyors, and items are restored correctly

5. **Mobile Compatibility (Primary Focus):**
   - Test MobileGridScene.unity thoroughly as the main gameplay scene
   - Verify touch controls work smoothly for placing/removing machines
   - Check UI scaling and responsiveness on different screen sizes and orientations
   - Test gesture controls and mobile input system
   - Verify performance optimization for mobile devices

6. **Resource Data Integrity:**
   - Verify items.json loads correctly (aluminum cans, shredded aluminum)
   - Verify machines.json loads correctly (spawner, shredder, seller, conveyor)
   - Verify recipes.json loads correctly (can → shreddedAluminum conversion)
   - Test missing or malformed JSON file handling

## Unit Testing Requirements (MANDATORY FOR ALL CHECK-INS)

### Test Framework Setup
ScrapLine uses Unity's Test Framework with NUnit for comprehensive unit testing. All unit tests MUST PASS before any code can be checked in.

**Test Assembly Definitions:**
- `Assets/Tests/Runtime/ScrapLine.Tests.Runtime.asmdef` - Runtime tests (play mode)
- `Assets/Tests/Editor/ScrapLine.Tests.Editor.asmdef` - Editor tests (edit mode)

### Running Unit Tests (CRITICAL - NEVER SKIP)
**ALL tests must pass for every check-in. NO EXCEPTIONS.**

#### Command Line Test Execution (Recommended)
```bash
# Run ALL tests (takes 5-15 minutes, NEVER CANCEL)
Unity -batchmode -quit -projectPath /home/runner/work/ScrapLine/ScrapLine -runTests -testPlatform PlayMode -testResults PlayModeResults.xml -logFile play_mode_tests.log

# Run Editor tests only (takes 2-5 minutes)
Unity -batchmode -quit -projectPath /home/runner/work/ScrapLine/ScrapLine -runTests -testPlatform EditMode -testResults EditModeResults.xml -logFile edit_mode_tests.log

# Alternative: Run both test modes in sequence
Unity -batchmode -quit -projectPath /home/runner/work/ScrapLine/ScrapLine -runTests -testResults AllTestResults.xml -logFile all_tests.log
```

#### Unity Editor Test Runner (Manual Testing)
```bash
# Open Unity Editor and use Test Runner window
Unity -projectPath /home/runner/work/ScrapLine/ScrapLine
# Then: Window > General > Test Runner > Run All (PlayMode and EditMode)
```

#### Quick Validation for Compilation Issues
```bash
# Fast syntax validation (30 seconds) - use when Unity is not available
cd /tmp && dotnet new console -o test-compile --force && cd test-compile
# Copy a few key C# files to check basic syntax
cp /home/runner/work/ScrapLine/ScrapLine/Assets/Scripts/Backend/Data/FactoryDefinitions.cs .
dotnet add package NUnit
dotnet build
```

### Test Coverage Requirements
**Current Test Coverage: 9/28 classes (32%) with 206 individual test methods**

#### Core Data Models (COMPLETE)
- ✅ FactoryDefinitionsTests.cs - Tests MachineDef, RecipeDef, ItemDef, UpgradeMultiplier (24 tests)
- ✅ GameDataTests.cs - Tests ItemData, CellData, GridData, UserMachineProgress (35 tests)
- ✅ FactoryRegistryTests.cs - Tests singleton, JSON loading, data retrieval (32 tests)

#### Manager Classes (COMPLETE)  
- ✅ CreditsManagerTests.cs - Tests credits system with mock UI components (19 tests)
- ✅ BaseMachineTests.cs - Tests abstract machine functionality (15 tests)

#### Machine Types (COMPLETE)
- ✅ ConveyorMachineTests.cs - Tests conveyor movement and failsafe logic (15 tests)
- ✅ SpawnerMachineTests.cs - Tests item spawning with timing simulation (22 tests)
- ✅ SellerMachineTests.cs - Tests item selling and credit awarding (20 tests)
- ✅ BlankCellMachineTests.cs - Tests timeout and temporary storage (24 tests)

#### Still Required (IN PROGRESS)
- 🔄 ProcessorMachineTests.cs - Complex item processing workflows
- 🔄 MachineFactoryTests.cs - Machine instantiation patterns
- 🔄 GameManagerTests.cs - Core game orchestration
- 🔄 GridManagerTests.cs - Grid operations and data management
- 🔄 UIGridManagerTests.cs - UI grid visualization
- 🔄 SaveLoadManagerTests.cs - Game state persistence
- 🔄 All other remaining classes...

### Mandatory Pre-Commit Test Execution
**EVERY developer MUST run these commands before committing:**

```bash
# Step 1: Run all unit tests (15-20 minutes total)
Unity -batchmode -quit -projectPath /home/runner/work/ScrapLine/ScrapLine -runTests -testResults TestResults.xml -logFile tests.log

# Step 2: Verify test results
if grep -q 'result="Failed"' TestResults.xml; then
    echo "❌ TESTS FAILED - Cannot commit changes"
    cat tests.log | grep -A 5 -B 5 "Failed"
    exit 1
else
    echo "✅ All tests passed - Safe to commit"
fi

# Step 3: Run JSON validation (quick safety check)
python3 -m json.tool /home/runner/work/ScrapLine/ScrapLine/Assets/Resources/items.json > /dev/null || exit 1
python3 -m json.tool /home/runner/work/ScrapLine/ScrapLine/Assets/Resources/machines.json > /dev/null || exit 1
python3 -m json.tool /home/runner/work/ScrapLine/ScrapLine/Assets/Resources/recipes.json > /dev/null || exit 1

echo "✅ All validations passed - Ready for commit"
```

### Test Quality Standards
**ALL tests must meet these requirements:**
- ✅ Use NUnit [TestFixture] and [Test] attributes
- ✅ Follow Arrange/Act/Assert pattern with clear comments
- ✅ Include comprehensive edge case testing (null values, invalid inputs)
- ✅ Use mock objects for Unity dependencies (GameManager, UI components)
- ✅ Test both success and failure scenarios
- ✅ Include integration tests for complex workflows
- ✅ NO placeholder code - all tests must be complete and runnable
- ✅ Descriptive test names explaining what is being tested

### Test Debugging and Troubleshooting
```bash
# View detailed test output for failures
cat tests.log | grep -A 10 -B 10 "Failed\|Error"

# Run specific test class only
Unity -batchmode -quit -projectPath /home/runner/work/ScrapLine/ScrapLine -runTests -testPlatform PlayMode -testFilter "ScrapLine.Tests.FactoryRegistryTests" -logFile specific_test.log

# Check test assembly compilation
Unity -batchmode -quit -projectPath /home/runner/work/ScrapLine/ScrapLine -executeMethod CompileTestAssemblies -logFile test_compile.log
```

### Continuous Integration Requirements
**For automated builds and CI systems:**
- Tests must complete within 30 minutes maximum
- Zero tolerance for test failures - build fails if ANY test fails
- Test results must be published to TestResults.xml for CI parsing
- Coverage reports should be generated for tracking progress

## Measured Command Timings (Validated in Current Environment)

### Actually Tested Commands and Timing
These commands have been validated to work in Ubuntu 24.04 with .NET 8.0.119:

```bash
# .NET project creation and build: 11 seconds (measured)
cd /tmp && dotnet new console -o unity-compile-test --force && cd unity-compile-test && dotnet build --verbosity minimal

# JSON validation: <1 second each (measured)
python3 -m json.tool /home/runner/work/ScrapLine/ScrapLine/Assets/Resources/items.json > /dev/null
python3 -m json.tool /home/runner/work/ScrapLine/ScrapLine/Assets/Resources/machines.json > /dev/null
python3 -m json.tool /home/runner/work/ScrapLine/ScrapLine/Assets/Resources/recipes.json > /dev/null

# File system exploration: <1 second (measured)
find /home/runner/work/ScrapLine/ScrapLine/Assets/Scripts -name "*.cs" | head -10
```

### Unity-Specific Timing Expectations (Industry Standard)
Based on Unity 6 LTS documentation and industry standards:
- **Project Opening:** 5-10 minutes (first time)
- **Script Compilation:** 1-2 minutes
- **Development Build:** 15-30 minutes
- **Release Build:** 30-45 minutes  
- **WebGL Build:** 45-90 minutes
- **Test Suite:** 5-15 minutes

### Memory and Performance
- **Editor Memory Usage:** 2-4 GB RAM typical
- **Build Requirements:** 8+ GB available disk space
- **Graphics:** DirectX 11/OpenGL 3.3+ or Metal support

## Troubleshooting Common Issues

### Build Failures
- **Shader compilation errors:** Clean and rebuild
- **Package resolution issues:** Delete Library folder, reopen project
- **Platform module missing:** Install target platform in Unity Hub

### Runtime Issues
- **Items not moving:** Check conveyor belt direction settings
- **UI not responding:** Verify EventSystem present in scene
- **Save data corruption:** Check JSON serialization in GameManager

### Performance Problems
- **Frame rate drops:** Use Unity Profiler to identify bottlenecks
- **Memory leaks:** Check object pooling in item spawning system

## Version Control Best Practices
- **NEVER commit:** `Library/`, `Temp/`, `Logs/`, `UserSettings/`, `*.tmp` files
- **ALWAYS commit:** `Assets/`, `ProjectSettings/`, `Packages/manifest.json`
- **Meta files:** Always commit `.meta` files alongside asset files

## Development Environment Notes
- **Unity Version:** 6000.2.3f1 (Unity 6 LTS) - EXACT version required
- **Target Platforms:** Mobile (iOS/Android primary), Standalone (Windows/Mac/Linux), WebGL
- **Render Pipeline:** Universal Render Pipeline (URP)
- **Input System:** New Unity Input System package
- **Graphics API:** DirectX 11, OpenGL, Metal, Vulkan
- **.NET Version:** Compatible with .NET 8.0+ for basic syntax validation

## Common Development Patterns in ScrapLine

### When Adding New Items:
1. Add item definition to `Assets/Resources/items.json`
2. Add corresponding sprite to `Assets/Resources/Sprites/`
3. Update item handling in `GameManager.cs` if needed
4. Test spawning and movement through conveyor system

### When Adding New Machines:
1. Add machine definition to `Assets/Resources/machines.json`
2. Define processing rules in `Assets/Resources/recipes.json`
3. Create machine behavior script inheriting from base machine class
4. Add UI button configuration in machine selection panel
5. Test placement restrictions and grid behavior

### When Modifying Grid System:
1. Always test in MobileGridScene.unity as the primary target platform
2. Verify UIGridManager.InitGrid() behavior for mobile interface
3. Check UICell placement and removal logic with touch controls
4. Test save/load functionality with new grid configurations
5. Ensure mobile performance optimization is maintained

### Performance Considerations:
- Item movement uses smooth interpolation, check `itemMoveSpeed` in GameManager
- Conveyor materials use shared materials for performance
- Grid cells are pooled objects, avoid creating/destroying frequently

## Central Logging System (MANDATORY)

**CRITICAL: Components MUST NOT use Debug.Log directly. All logging must go through the central LoggingManager.**

### Logging Rules (STRICTLY ENFORCED)
1. **NEVER use Debug.Log, Debug.LogWarning, or Debug.LogError directly in any component**
2. **ALWAYS use GameLogger static methods for all logging**
3. **ALWAYS provide a componentId for state-change detection when logging from machines/managers**
4. **ALWAYS call GameLogger.NotifyStateChange() when component state changes significantly**

### Required Logging Usage Patterns

#### Basic Logging:
```csharp
// Use category-specific convenience methods
GameLogger.LogMovement("Item moved to new position", componentId);
GameLogger.LogFabricator("Recipe processing started", componentId);
GameLogger.LogProcessor("Shredding aluminum can", componentId);
GameLogger.LogGrid("Cell placement validated", componentId);
GameLogger.LogMachine("Machine upgraded successfully", componentId);
GameLogger.LogEconomy("Credits awarded for sale", componentId);

// Or use generic method with category
GameLogger.Log(LoggingManager.LogCategory.Movement, "Custom message", componentId);
GameLogger.LogWarning(LoggingManager.LogCategory.Grid, "Invalid placement", componentId);
GameLogger.LogError(LoggingManager.LogCategory.SaveLoad, "File corrupted", componentId);
```

#### Component ID Format:
```csharp
// For machines: "MachineType_X_Y" (grid coordinates)
private string ComponentId => $"Fabricator_{cellData.x}_{cellData.y}";
private string ComponentId => $"Conveyor_{cellData.x}_{cellData.y}";

// For managers: "ManagerName_InstanceId" 
private string ComponentId => $"GridManager_{GetInstanceID()}";
private string ComponentId => $"SaveLoadManager_{GetInstanceID()}";

// For UI: "UIComponent_SpecificId"
private string ComponentId => $"MachineButton_{machineDefId}";
```

#### State Change Notification:
```csharp
// Call when significant state changes occur
cellData.machineState = MachineState.Processing;
GameLogger.NotifyStateChange(ComponentId); // Allows next log to show
GameLogger.LogFabricator("Started processing recipe", ComponentId);
```

### Available Log Categories:
- **Movement**: Item movement, conveyor logic, pathfinding
- **Fabricator**: Complex recipe processing, multi-input logic  
- **Processor**: Single-input processing (shredders, granulators)
- **Grid**: Cell placement, rotation, validation, highlighting
- **UI**: Button clicks, state changes, user interactions
- **SaveLoad**: Data persistence, file operations, serialization
- **Machine**: Placement, upgrades, state changes, refunds
- **Economy**: Credits, costs, transactions, balance changes
- **Spawning**: Item generation, spawn timing, spawn limits
- **Selling**: Item removal, credit awarding, sell validation
- **Debug**: General debugging, test scenarios, validation

### LoggingManager Configuration:
- Runtime enable/disable per category via Unity Inspector
- State-change detection prevents duplicate messages
- Configurable timeout for duplicate suppression
- Timestamps and category prefixes available
- Performance optimized with early exit when categories disabled

### Testing Logging:
```csharp
// Check if category is enabled before expensive string operations
if (GameLogger.IsCategoryEnabled(LoggingManager.LogCategory.Movement))
{
    string expensiveDebugInfo = GenerateDetailedMovementReport();
    GameLogger.LogMovement(expensiveDebugInfo, ComponentId);
}

// Enable/disable categories at runtime for debugging
GameLogger.SetCategoryEnabled(LoggingManager.LogCategory.Fabricator, true);
```

### Violation Detection:
- Any PR using Debug.Log* directly will be rejected
- Components must use GameLogger for ALL logging
- Missing componentId for machine/manager logging is a code review violation
- Failure to call NotifyStateChange for significant state changes reduces debugging effectiveness

**FINAL REMINDER: Unity operations are time-intensive. NEVER cancel builds, imports, or compilation processes. Always allow adequate time for completion and set appropriate timeouts (60+ minutes for builds, 30+ minutes for tests).**
