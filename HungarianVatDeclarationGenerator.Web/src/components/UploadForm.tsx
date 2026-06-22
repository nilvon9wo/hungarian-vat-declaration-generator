/** @jsxImportSource react */
import type { ChangeEvent, SubmitEvent } from 'react';
import { ErrorMessage } from './ErrorMessage';

interface UploadFormProps {
  selectedFile: File | null;
  isLoading: boolean;
  error: string | null;
  onFileChange: (event: ChangeEvent<HTMLInputElement>) => void;
  onSubmit: (event: SubmitEvent<HTMLFormElement>) => void;
}

export function UploadForm({ selectedFile, isLoading, error, onFileChange, onSubmit }: UploadFormProps) {
  return (
    <section className="vat-app__upload-section upload-form">
      <form className="upload-form__form" onSubmit={onSubmit}>
        <div className="upload-form__field">
          <label htmlFor="csv-file" className="upload-form__label">
            Upload CSV File:
          </label>
          <input
            id="csv-file"
            type="file"
            accept=".csv,text/csv"
            onChange={onFileChange}
            disabled={isLoading}
            className="upload-form__input"
          />
        </div>

        <button type="submit" disabled={!selectedFile || isLoading} className="upload-form__submit">
          {isLoading ? 'Processing...' : 'Generate VAT Declaration'}
        </button>
      </form>

      {error && <ErrorMessage message={error} />}
    </section>
  );
}
