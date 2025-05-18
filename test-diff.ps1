# Test script for DiffMore with color-coded diff

# Check if test files exist
if (-not (Test-Path -Path "test1.txt") -or -not (Test-Path -Path "test2.txt")) {
    # Create test files if they don't exist
    $testContent1 = @"
Line 1 - Same in both files
Line 2 - Same in both files
Line 3 - This line will be different
Line 4 - This line exists in both
Line 5 - This line exists in both
Line 6 - Another different line
Line 7 - Same in both files
Line 8 - To be deleted in test2
Line 9 - Same in both files
Line 10 - Same in both files
"@

    $testContent2 = @"
Line 1 - Same in both files
Line 2 - Same in both files
Line 3 - This line has been changed
Line 4 - This line exists in both
Line 5 - This line exists in both
Line 6 - This line has been modified too
Line 7 - Same in both files
Line 9 - Same in both files
Line 10 - Same in both files
Line 11 - New line added in test2
"@

    Set-Content -Path "test1.txt" -Value $testContent1
    Set-Content -Path "test2.txt" -Value $testContent2
    Write-Host "Created test files with sample content."
}

# Build the solution if needed
if (-not (Test-Path -Path "DiffMore.CLI\bin\Debug\net9.0\DiffMore.CLI.dll")) {
    Write-Host "Building the solution..."
    dotnet build
}

# Run the diff with color support
Write-Host "Running DiffMore with color support to compare test files..."
Write-Host
dotnet run --project .\DiffMore.CLI\DiffMore.CLI.csproj . test*.txt 