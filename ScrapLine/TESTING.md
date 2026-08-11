# Unity tests and content validation

The EditMode suite lives in `Assets/Tests/EditMode`. Open **Window > General > Test Runner**, select
**EditMode**, and choose **Run All** to run it in the Unity Editor.

Content definitions can also be checked without entering Play Mode by choosing
**ScrapLine > Validate Content Data**. Errors identify the source file, owning ID, field, and broken
reference or constraint.

## Batch mode

Run the suite from the repository root on Windows (adjust the Unity version/path if needed):

```powershell
& 'C:\Program Files\Unity\Hub\Editor\6000.5.3f1\Editor\Unity.exe' `
  -batchmode -nographics `
  -projectPath '.\ScrapLine' `
  -runTests -testPlatform EditMode `
  -testResults '.\ScrapLine\Logs\editmode-results.xml' `
  -logFile '.\ScrapLine\Logs\editmode-tests.log'
```

Run only content validation (this exits unsuccessfully when errors are found):

```powershell
& 'C:\Program Files\Unity\Hub\Editor\6000.5.3f1\Editor\Unity.exe' `
  -batchmode -nographics -quit `
  -projectPath '.\ScrapLine' `
  -executeMethod ScrapLine.Editor.ContentValidation.ContentValidationMenu.ValidateContent `
  -logFile '.\ScrapLine\Logs\content-validation.log'
```

## CI readiness

The batch command is suitable for CI and writes NUnit-compatible XML. A GitHub Actions workflow is
intentionally deferred until Unity licensing is configured. A future workflow should provide a Unity
license using repository secrets (for example `UNITY_LICENSE`, plus `UNITY_EMAIL` and
`UNITY_PASSWORD` when required by the chosen activation method) and must not print those values.
