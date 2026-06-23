# Using the Hungarian VAT Declaration API

## Authentication

This API uses a **demo-only** API key for authentication. **DO NOT use this pattern in production.**

### API Key
- **Header Name:** `X-API-Key`
- **Demo Value:** `challenge-demo-key-2024`

## Using Swagger UI

1. **Start the backend:**
   ```powershell
   cd HungarianVatDeclarationGenerator.Api
   dotnet run
   ```

2. **Open Swagger:** Navigate to `https://localhost:7122` in your browser

3. **Authenticate (for protected endpoints):**
   - Click the **🔓 Authorize** button (top right of Swagger UI)
   - Enter API Key: `challenge-demo-key-2024`
   - Click **Authorize**, then **Close**

4. **Test endpoints:**
   - **Public endpoint:** `/api/Config` - Works without authentication
   - **Protected endpoints:** `/api/VatDeclaration/*` - Require API key (padlock icon will show on these)

5. **Execute requests:**
   - Click "Try it out" on any endpoint
   - Fill in parameters (if any)
   - Click "Execute"
   - The API key is automatically included for protected endpoints after you authorize

### Swagger Features

✅ **Authorize button** - Global authentication that applies to all protected endpoints
✅ **Padlock icons** - Visual indicator showing which endpoints require authentication  
✅ **Automatic header injection** - After authorizing, the `X-API-Key` header is added automatically

## Using the Frontend

1. **Ensure `.env` file exists** in `HungarianVatDeclarationGenerator.Web/`:
   ```
   VITE_API_KEY=challenge-demo-key-2024
   ```

2. **Start the frontend:**
   ```powershell
   cd HungarianVatDeclarationGenerator.Web
   npm run dev
   ```

3. **Access the app:** Navigate to `http://localhost:5173`

The frontend automatically includes the API key from `.env` in all requests.

## Public vs Protected Endpoints

### Public (No Auth Required)
- `GET /api/Config` - Returns upload limits and allowed file types

### Protected (API Key Required)
- `POST /api/VatDeclaration/upload` - Upload and process CSV
- `POST /api/VatDeclaration/upload-and-generate-pdf` - Upload CSV and generate PDF

## Testing the API

### Using curl

**Public endpoint:**
```bash
curl -k https://localhost:7122/api/Config
```

**Protected endpoint:**
```bash
curl -k -X POST https://localhost:7122/api/VatDeclaration/upload \
  -H "X-API-Key: challenge-demo-key-2024" \
  -H "Content-Type: multipart/form-data" \
  -F "file=@invoices.csv"
```

### Using PowerShell

**Public endpoint:**
```powershell
Invoke-RestMethod -Uri "https://localhost:7122/api/Config" -SkipCertificateCheck
```

**Protected endpoint:**
```powershell
$headers = @{ "X-API-Key" = "challenge-demo-key-2024" }
$form = @{ file = Get-Item -Path "invoices.csv" }
Invoke-RestMethod -Uri "https://localhost:7122/api/VatDeclaration/upload" `
  -Method Post -Headers $headers -Form $form -SkipCertificateCheck
```

## Why Is Config Public?

The `/api/Config` endpoint does not require authentication because:
1. It only exposes non-sensitive configuration (file size limits, allowed extensions)
2. The frontend needs this data **before** the user interacts with the app
3. It contains no business data or user information

## Security Notes

⚠️ **This is a demo implementation for a coding challenge only.**

**DO NOT use in production:**
- API keys should never be in source control
- Use proper JWT tokens with Azure AD, Auth0, or similar
- Store secrets in Azure Key Vault or similar secret management
- Implement proper RBAC and claims-based authorization
- Use HTTPS with valid certificates (not self-signed)

## Troubleshooting

### "Configuration not loaded" error in frontend
- Verify the backend is running on `https://localhost:7122`
- Check that `/api/Config` endpoint is accessible without auth
- Verify the frontend proxy in `vite.config.ts` is correct

### "401 Unauthorized" in Swagger
- Ensure you've added the `X-API-Key` header to the request
- Verify the key value is exactly `challenge-demo-key-2024`
- Check the endpoint requires auth (Config endpoint doesn't)

### "502 Bad Gateway" in frontend
- Check the backend is running
- Verify the proxy target in `vite.config.ts` matches the backend HTTPS port (7122)
