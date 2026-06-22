import { useState, type ChangeEvent, type SubmitEvent } from 'react';
import type { VatDeclarationResult } from './types/api';
import { uploadCsvFile, downloadPdf } from './services/api';
import { UploadForm } from './components/UploadForm';
import { VatResults } from './components/VatResults';
import { triggerBrowserDownload } from './utils/download';
import './App.css';

function App() {
  const [selectedFile, setSelectedFile] = useState<File | null>(null);
  const [isLoading, setIsLoading] = useState<boolean>(false);
  const [error, setError] = useState<string | null>(null);
  const [result, setResult] = useState<VatDeclarationResult | null>(null);

  async function handleSubmit(event: SubmitEvent<HTMLFormElement>) {
    event.preventDefault();

    if (validateFileSelection()) {
      await processUpload();
    }
  }

  async function handleDownloadPdf(): Promise<void> {
    setIsLoading(true);
    setError(null);

    await processPdfDownload();

    setIsLoading(false);
  }

  return (
    <div className="vat-app">
      <header className="vat-app__header">
        <h1 className="vat-app__title">Hungarian VAT Declaration Generator</h1>
      </header>

      <main className="vat-app__main">
        <UploadForm
          selectedFile={selectedFile}
          isLoading={isLoading}
          error={error}
          onFileChange={handleFileChange}
          onSubmit={handleSubmit}
        />

        {result && <VatResults result={result} isLoading={isLoading} onDownloadPdf={handleDownloadPdf} />}
      </main>
    </div>
  );

  function handleFileChange(event: ChangeEvent<HTMLInputElement>): void {
    const file: File | null = event.target.files?.[0] || null;
    setSelectedFile(file);
    clearResults();
  }

  function clearResults(): void {
    setError(null);
    setResult(null);
  }

  function validateFileSelection(): boolean {
    if (!selectedFile) {
      setError('Please select a CSV file');
      return false;
    }
    return true;
  }

  async function processUpload(): Promise<void> {
    if (!selectedFile) return;

    setIsLoading(true);
    clearResults();

    const uploadResult = await uploadCsvFile(selectedFile);

    setIsLoading(false);

    if (uploadResult.success && uploadResult.data) {
      setResult(uploadResult.data);
    } else {
      setError(uploadResult.error || 'An unknown error occurred');
    }
  }

  async function processPdfDownload(): Promise<void> {
    if (!selectedFile) return;

    try {
      const blob: Blob = (await downloadPdf(selectedFile)) as Blob;
      triggerBrowserDownload(blob, 'vat-declaration.pdf');
    } catch (err: unknown) {
      const errorMessage: string = err instanceof Error ? err.message : 'Failed to download PDF';
      setError(errorMessage);
    }
  }
}

export default App;
