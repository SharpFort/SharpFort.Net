# CS0006 Troubleshooting Guide: "Metadata file could not be found"

## 1. What is this error?
**Error Code:** `CS0006`
**Message:** "Metadata file '.../bin/Debug/net8.0/ProjectName.dll' could not be found"

This error means the compiler cannot find the compiled DLL (`.dll` file) of a project that your current project depends on.

**It is almost always a SYMPTOM, not the root cause.**

Think of it like this: Project A depends on Project B. If Project B fails to compile (due to a syntax error, missing semicolon, etc.), it never produces a `ProjectB.dll`. Consequently, Project A complains: "I can't find ProjectB.dll!" (CS0006).

## 2. Common Causes

1.  **Build Failure in Dependent Project (Most Common)**
    *   The project you are referencing has compilation errors itself.
    *   *Example:* `Yi.Abp.Web` depends on `Yi.Framework.CasbinRbac`. If `CasbinRbac` has a syntax error, `Yi.Abp.Web` fails with CS0006.

2.  **Target Framework Mismatch**
    *   Referencing a `.NET 8` project from a `.NET 6` project, or incompatible platforms (e.g., `x64` vs `Any CPU`).

3.  **Stale Intermediate Files**
    *   Detailed build info in `obj` folders is out of sync with the actual code, often happening after switching branches or large refactors.

4.  **Visual Studio / IDE Glitch**
    *   The IDE's in-memory view of the project structure differs from the file system.

## 3. How to Solve It (Development Workflow)

Follow these steps in order when you see CS0006.

### Step 1: Ignore the "Metadata" Error, Look for the *Real* Error
The CS0006 error is noise. Look at the **Output Window** (not just the Error List) or search for other red errors in the file list.
*   **Action**: Find the project listed in the error message (e.g., if it says "Metadata file 'ProjectB.dll' not found", go look at **ProjectB**).
*   **Action**: Try to build **ONLY** that referenced project.

### Step 2: Build the Dependency Chain Manually
Don't build the whole solution immediately. Build from the bottom up.
*   **Command**:
    ```powershell
    dotnet build path/to/DependentProject.csproj
    ```
*   *Why?* This isolates the actual compilation error (like a missing semicolon or interface mismatch) preventing the DLL from being created.

### Step 3: Clean and Rebuild
If individual projects build fine but the solution still fails, the build cache might be corrupt.
*   **Command**:
    ```powershell
    # Nuke the temporary folders
    dotnet clean
    # Or for a deep clean (PowerShell):
    Get-ChildItem -Inc -Include bin,obj -Recurse | Remove-Item -Force -Recurse
    
    # Restore dependencies and build
    dotnet restore
    dotnet build
    ```

### Step 4: Check Configuration
Ensure all projects in the solution are targeting the same framework version and build configuration (Debug/Release).

## 4. Summary Checklist

| Context | Action | Command |
| :--- | :--- | :--- |
| **First Sign** | Identify the missing DLL's project. | (Eye check error message) |
| **Investigation** | Build *only* that failing dependency. | `dotnet build <ProjectB_Path>` |
| **Persistent Error** | Clear stale caches. | `dotnet clean` |
| **Nuclear Option** | Delete all `bin`/`obj` folders. | `rw -r bin/ obj/` (Linux/Mac) or PS script |

---
**Key Takeaway**: When you see "Metadata file not found", stop looking at the project that *has* the error, and start fixing the project it *references*.
