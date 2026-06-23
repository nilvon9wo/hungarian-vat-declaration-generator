# Quick Start Guide - API Key Authentication

## For Demo/Testing Purposes

### API Key
```
X-API-Key: challenge-demo-key-2024
```

⚠️ **This is a demo key for the coding challenge. In production, use JWT tokens with proper authentication infrastructure.**

---

## Backend Configuration

### Location
`HungarianVatDeclarationGenerator.Api/appsettings.json`

```json
{
  "ApiKey": {
	"HeaderName": "X-API-Key",
	"ValidKey": "challenge-demo-key-2024"
  }
}
```

---

## Frontend Configuration

### Location
`HungarianVatDeclarationGenerator.Web/.env`

```env
VITE_API_KEY=challenge-demo-key-2024
```

**Note:** The `.env` file is already created. If missing, copy from `.env.template`.

---

## Testing with Swagger

1. Start the API:
   ```powershell
   cd HungarianVatDeclarationGenerator.Api
   dotnet run
   ```

2. Open browser: `https://localhost:7122/`

3. **Authenticate once for all protected endpoints:**
   - Click the **🔓 Authorize** button (top right of Swagger UI)
   - Enter API Key: `challenge-demo-key-2024`
   - Click **Authorize**, then **Close**

4. Now test any endpoint:
   - Click "Try it out" on any endpoint
   - Upload your CSV file and execute
   - The API key is automatically included after authorization

---

## Testing with curl

### Upload CSV for VAT Declaration
```bash
curl -X POST https://localhost:7122/api/VatDeclaration/upload \
  -H "X-API-Key: challenge-demo-key-2024" \
  -F "file=@your-file.csv"
```

### Generate PDF Report
```bash
curl -X POST https://localhost:7122/api/VatDeclaration/upload-and-generate-pdf \
  -H "X-API-Key: challenge-demo-key-2024" \
  -F "file=@your-file.csv" \
  -o vat-declaration.pdf
```

---

## Testing with Frontend

1. Ensure `.env` exists with API key:
   ```env
   VITE_API_KEY=challenge-demo-key-2024
   ```

2. Start frontend:
   ```powershell
   cd HungarianVatDeclarationGenerator.Web
   npm run dev
   ```

3. Open browser: `http://localhost:5173`

4. Upload CSV - **API key is automatically included in all requests**

---

## What Happens Without API Key?

### Request without API key:
```bash
curl -X POST https://localhost:7122/api/VatDeclaration/upload \
  -F "file=@test.csv"
```

### Response:
```
HTTP 401 Unauthorized
{
  "error": "Missing API key header"
}
```

---

## Rate Limiting

### Limits
- **10 requests per minute** per IP for VAT endpoints
- **100 requests per hour** per IP globally

### When exceeded:
```
HTTP 429 Too Many Requests
{
  "message": "Rate limit exceeded. Please try again later."
}
```

---

## Frontend Automatic Behavior

The frontend automatically:
1. ✅ Reads API key from `.env` file
2. ✅ Adds `X-API-Key` header to all requests
3. ✅ Validates file size (5MB max) before upload
4. ✅ Validates file type (CSV only) before upload
5. ✅ Times out requests after 60 seconds
6. ✅ Shows user-friendly error messages

You don't need to do anything extra - just upload your CSV!

---

## Production Recommendations

### DO NOT use this approach in production:
❌ API keys in config files  
❌ API keys in source control  
❌ API keys in environment variables (for web apps)  

### Instead, use:
✅ JWT tokens with authentication server (Azure AD, Auth0, IdentityServer)  
✅ Secret management (Azure Key Vault, AWS Secrets Manager)  
✅ OAuth 2.0 / OpenID Connect  
✅ Proper token rotation and expiration  
✅ Role-based access control (RBAC)  

---

## Troubleshooting

### Frontend: "API key is missing"
1. Check `.env` file exists in `HungarianVatDeclarationGenerator.Web/`
2. Verify it contains: `VITE_API_KEY=challenge-demo-key-2024`
3. Restart Vite dev server after changing `.env`

### Swagger: 401 Unauthorized
1. Make sure you added the `X-API-Key` header manually
2. Check the value is exactly: `challenge-demo-key-2024` (no quotes)
3. Try the curl command to verify backend is working

### Backend: Configuration error
1. Check `appsettings.json` has `ApiKey` section
2. Verify the key matches: `challenge-demo-key-2024`
3. Restart the API after configuration changes

---

## Testing Checklist

- [ ] Backend builds successfully (`dotnet build`)
- [ ] Backend tests pass (22/22: `dotnet test`)
- [ ] Frontend builds successfully (`npm run build`)
- [ ] Frontend tests pass (20/20: `npm test -- --run`)
- [ ] Can access Swagger UI at `https://localhost:7122/`
- [ ] Can manually add API key in Swagger and upload CSV
- [ ] Can access frontend at `http://localhost:5173`
- [ ] Can upload CSV through frontend UI
- [ ] Rate limiting kicks in after 10 requests in 1 minute
- [ ] Requests without API key return 401 Unauthorized

---

**Everything should work out of the box!** The API key is pre-configured in both backend and frontend. Just run both projects and test. 🚀
