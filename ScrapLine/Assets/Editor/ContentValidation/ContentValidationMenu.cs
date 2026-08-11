using System;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace ScrapLine.Editor.ContentValidation
{
    public static class ContentValidationMenu
    {
        [MenuItem("ScrapLine/Validate Content Data")]
        public static void ValidateContent()
        {
            ContentValidationResult result = ContentDataValidator.ValidateProject();
            if (result.IsValid)
            {
                Debug.Log("CONTENT_DATA_VALIDATION_PASSED: items.json, machines.json, recipes.json, and wastecrates.json are valid.");
                return;
            }

            foreach (ContentValidationError error in result.Errors)
                Debug.LogError($"CONTENT_DATA_VALIDATION_ERROR: {error}");

            throw new InvalidOperationException(
                $"Content data validation failed with {result.Errors.Count} error(s):\n" +
                string.Join("\n", result.Errors.Select(error => error.ToString())));
        }
    }
}
