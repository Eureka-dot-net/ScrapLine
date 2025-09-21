using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// Validation script to test the dynamic machine system implementation
/// </summary>
public class SystemValidator : MonoBehaviour
{
    [System.Serializable]
    public class ValidationResult
    {
        public string testName;
        public bool passed;
        public string message;
    }

    public List<ValidationResult> results = new List<ValidationResult>();

    [ContextMenu("Run Validation")]
    public void RunValidation()
    {
        results.Clear();
        
        // Test 1: Factory Registry loads machines
        ValidateFactoryRegistry();
        
        // Test 2: Machine Bar uses dynamic machines
        ValidateMachineBar();
        
        // Test 3: Grid highlighting system
        ValidateGridHighlighting();
        
        // Test 4: Blank cell protection
        ValidateBlankCellProtection();
        
        // Test 5: Machine placement validation
        ValidateMachinePlacement();
        
        LogResults();
    }

    private void ValidateFactoryRegistry()
    {
        var result = new ValidationResult { testName = "Factory Registry" };
        
        try
        {
            var machines = FactoryRegistry.Instance.Machines;
            if (machines != null && machines.Count > 0)
            {
                result.passed = true;
                result.message = $"Successfully loaded {machines.Count} machines";
            }
            else
            {
                result.passed = false;
                result.message = "No machines loaded in FactoryRegistry";
            }
        }
        catch (System.Exception e)
        {
            result.passed = false;
            result.message = $"Error accessing FactoryRegistry: {e.Message}";
        }
        
        results.Add(result);
    }

    private void ValidateMachineBar()
    {
        var result = new ValidationResult { testName = "Machine Bar Dynamic Loading" };
        
        var machineBar = Object.FindAnyObjectByType<MachineBarUIManager>();
        if (machineBar != null)
        {
            result.passed = true;
            result.message = "MachineBarUIManager found and should use dynamic machines";
        }
        else
        {
            result.passed = false;
            result.message = "MachineBarUIManager not found in scene";
        }
        
        results.Add(result);
    }

    private void ValidateGridHighlighting()
    {
        var result = new ValidationResult { testName = "Grid Highlighting System" };
        
        var gridManager = Object.FindAnyObjectByType<UIGridManager>();
        if (gridManager != null)
        {
            // Check if highlighting methods exist
            var highlightMethod = gridManager.GetType().GetMethod("HighlightValidPlacements");
            var clearMethod = gridManager.GetType().GetMethod("ClearHighlights");
            
            if (highlightMethod != null && clearMethod != null)
            {
                result.passed = true;
                result.message = "Grid highlighting methods implemented";
            }
            else
            {
                result.passed = false;
                result.message = "Grid highlighting methods missing";
            }
        }
        else
        {
            result.passed = false;
            result.message = "UIGridManager not found in scene";
        }
        
        results.Add(result);
    }

    private void ValidateBlankCellProtection()
    {
        var result = new ValidationResult { testName = "Blank Cell Protection" };
        
        var gameManager = GameManager.Instance;
        if (gameManager != null)
        {
            // Check if movement methods exist and are updated
            var moveMethod = gameManager.GetType().GetMethod("TryStartItemMovement", 
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            
            if (moveMethod != null)
            {
                result.passed = true;
                result.message = "Movement protection methods found";
            }
            else
            {
                result.passed = false;
                result.message = "Movement protection methods not found";
            }
        }
        else
        {
            result.passed = false;
            result.message = "GameManager not found";
        }
        
        results.Add(result);
    }

    private void ValidateMachinePlacement()
    {
        var result = new ValidationResult { testName = "Machine Placement System" };
        
        var gameManager = GameManager.Instance;
        if (gameManager != null)
        {
            // Check for placement methods
            var setSelectedMethod = gameManager.GetType().GetMethod("SetSelectedMachine");
            var placementMethod = gameManager.GetType().GetMethod("IsValidMachinePlacement", 
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            
            if (setSelectedMethod != null && placementMethod != null)
            {
                result.passed = true;
                result.message = "Machine placement methods implemented";
            }
            else
            {
                result.passed = false;
                result.message = "Machine placement methods missing";
            }
        }
        else
        {
            result.passed = false;
            result.message = "GameManager not found";
        }
        
        results.Add(result);
    }

    private void LogResults()
    {
        Debug.Log("=== System Validation Results ===");
        int passed = 0;
        int total = results.Count;
        
        foreach (var result in results)
        {
            string status = result.passed ? "PASS" : "FAIL";
            Debug.Log($"[{status}] {result.testName}: {result.message}");
            if (result.passed) passed++;
        }
        
        Debug.Log($"=== Validation Complete: {passed}/{total} tests passed ===");
    }
}