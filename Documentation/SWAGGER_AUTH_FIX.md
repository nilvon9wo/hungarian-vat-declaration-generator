# Swagger Authentication 401 Fix

## Problem
Swagger UI's Authorize button is not properly sending the X-API-Key header, resulting in 401 Unauthorized responses when trying to upload CSV files.

## Root Cause
The OpenAPI security scheme had an unnecessary `Scheme` property that may interfere with how Swagger UI sends API key authentication headers.

## Changes Made

### 1. Fixed Security Scheme Definition
**File:** `HungarianVatDeclarationGenerator.Api/Program.cs`

**Before:**
```csharp
options.AddSecurityDefinition("ApiKey", new OpenApiSecurityScheme
{
	Description = "API Key Authentication. Enter your API key below.",
	Name = "X-API-Key",
	In = ParameterLocation.Header,
	Type = SecuritySchemeType.ApiKey,
	Scheme = "ApiKeyScheme"  // ❌ This shouldn't be here for API keys
});
```

**After:**
```csharp
options.AddSecurityDefinition("ApiKey", new OpenApiSecurityScheme
{
	Description = "API Key Authentication. Enter your API key below.",
	Name = "X-API-Key",
	In = ParameterLocation.Header,
	Type = SecuritySchemeType.ApiKey  // ✅ No Scheme property
});
```

**Rationale:** In OpenAPI 3.0, the `scheme` property is only used for HTTP authentication (Bearer, Basic, etc.). For `apiKey` type security, it should not be set as it can cause the Swagger UI to not send the header correctly.

### 2. Added Authentication Logging
**File:** `HungarianVatDeclarationGenerator.Api/Authentication/ApiKeyAuthenticationHandler.cs`

Added detailed logging to help diagnose authentication failures:
- Logs when the header is missing
- Logs when the header is empty
- Logs when the API key is invalid
- Logs successful authentication

This will help you see exactly what's happening in the backend logs when authentication fails.

## How to Test

1. **Stop the currently running backend**
   ```powershell
   # Stop the process (Ctrl+C in the terminal or stop from VS)
   ```

2. **Start the backend with the fixed code**
   ```powershell
   cd HungarianVatDeclarationGenerator.Api
   dotnet run
   ```

3. **Open Swagger UI**
   - Navigate to `https://localhost:7122`

4. **Authenticate**
   - Click the 🔓 **Authorize** button (top right)
   - Enter: `challenge-demo-key-2024`
   - Click **Authorize**, then **Close**

5. **Test CSV Upload**
   - Navigate to `/api/VatDeclaration/upload`
   - Click **Try it out**
   - Upload a CSV file
   - Click **Execute**

6. **Check Logs**
   If it still fails, check the backend console output for log messages like:
   ```
   API key header 'X-API-Key' not found in request to /api/VatDeclaration/upload
   ```
   or
   ```
   Invalid API key provided in request to /api/VatDeclaration/upload
   ```

## Expected Behavior

After the fix:
- ✅ Clicking Authorize and entering the API key should work
- ✅ Protected endpoints should show the padlock icon as **locked** (filled)
- ✅ Requests to protected endpoints should include `X-API-Key` header automatically
- ✅ You should get 200/201 responses instead of 401

## If It Still Doesn't Work

Check these things:

1. **Verify the API key in appsettings.json**
   ```json
   "ApiKey": {
	 "HeaderName": "X-API-Key",
	 "ValidKey": "challenge-demo-key-2024"
   }
   ```

2. **Check browser console** (F12 Developer Tools)
   - Look at the Network tab when you execute a request
   - Check the Request Headers - ensure `X-API-Key: challenge-demo-key-2024` is present

3. **Check backend logs**
   - Look for the authentication log messages we added
   - They will tell you exactly why authentication is failing

4. **Try manually adding the header**
   - In Swagger, after clicking "Try it out"
   - Look for a "Parameters" or "Headers" section
   - Manually add: `X-API-Key` = `challenge-demo-key-2024`
   - This will tell us if it's a Swagger UI issue or a backend issue

## Alternative: Use Browser Extensions

If Swagger UI continues to have issues, you can use:
- **Postman** - Import the Swagger JSON and test from there
- **curl** - Command line testing (see USING_THE_API.md)
- **PowerShell** `Invoke-RestMethod` - Script-based testing

All of these let you explicitly control the headers being sent.
