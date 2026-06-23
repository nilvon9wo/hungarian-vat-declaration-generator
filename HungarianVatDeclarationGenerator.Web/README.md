# Hungarian VAT Declaration Generator Web

React + TypeScript frontend for uploading invoice CSV files and displaying the VAT summary returned by the API.

## Local development

- UI: `http://localhost:5173`
- API proxy target: `https://localhost:7122`
- Demo API key source: `HungarianVatDeclarationGenerator.Web/.env`

Start the frontend from this folder:

```bash
npm install
npm run dev
```

## Backend dependency

The Vite dev server proxies `/api/*` requests to the ASP.NET Core backend defined in `vite.config.ts`.

Swagger and local backend access are available at `https://localhost:7122`.

## Validation behavior

The frontend fetches upload limits from `GET /api/Config` and applies matching client-side checks for file extension and file size before upload.

## Useful commands

```bash
npm run lint
npm run test -- --run
npm run build
```
