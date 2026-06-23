# Hungarian VAT Declaration Generator API

ASP.NET Core backend for CSV upload, VAT calculation, configuration lookup, and optional PDF generation.

## Local development

- Swagger and HTTPS backend: `https://localhost:7122`
- HTTP profile: `http://localhost:5247`

Run the API from the repository root:

```bash
dotnet run --project HungarianVatDeclarationGenerator.Api
```

Swagger is configured at the application root, so opening `https://localhost:7122` loads the API UI directly.

## Notes

- `GET /api/Config` is public so the frontend can load upload limits before submission.
- `POST /api/VatDeclaration/*` endpoints use the demo `X-API-Key` flow documented in `../Documents/UserGuide/USING_THE_API.md`.
- Automated backend tests live in `../HungarianVatDeclarationGenerator.Api.Tests`.
