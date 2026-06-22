import type { VatDeclarationResult, ApiError } from '../types/api';

const API_BASE_URL = '/api';

export interface UploadResult {
  success: boolean;
  data?: VatDeclarationResult;
  error?: string;
}

export async function uploadCsvFile(file: File): Promise<UploadResult> {
  const formData: FormData = createFormData(file);

  try {
    const response: Response = await fetch(`${API_BASE_URL}/VatDeclaration/upload`, {
      method: 'POST',
      body: formData,
    });

    if (!response.ok) {
      const error: string = await handleErrorResponse(response);
      return { success: false, error };
    }

    const data: VatDeclarationResult = await response.json();
    return createSuccessResult(data);
  } catch (error: unknown) {
    return createErrorResult(error);
  }
}

export async function downloadPdf(file: File): Promise<Blob | string> {
  const formData: FormData = createFormData(file);

  try {
    return await fetchPdfBlob(formData);
  } catch (error: unknown) {
    return wrapPdfError(error);
  }
}

function createFormData(file: File): FormData {
  const formData: FormData = new FormData();
  formData.append('file', file);
  return formData;
}

async function handleErrorResponse(response: Response): Promise<string> {
  const errorData: ApiError = await response.json().catch(() => ({ 
    error: `HTTP ${response.status}: ${response.statusText}` 
  }));
  return errorData.error || `HTTP ${response.status}: ${response.statusText}`;
}

function createSuccessResult(data: VatDeclarationResult): UploadResult {
  return {
    success: true,
    data,
  };
}

function createErrorResult(error: unknown): UploadResult {
  const errorMessage: string = error instanceof Error 
    ? error.message 
    : 'Network error occurred';

  return {
    success: false,
    error: `Failed to upload file: ${errorMessage}`,
  };
}

async function fetchPdfBlob(formData: FormData): Promise<Blob> {
  const response: Response = await fetch(`${API_BASE_URL}/VatDeclaration/upload-and-generate-pdf`, {
    method: 'POST',
    body: formData,
  });

  if (!response.ok) {
    const error: string = await handleErrorResponse(response);
    throw new Error(error);
  }

  return await response.blob();
}

function wrapPdfError(error: unknown): never {
  const errorMessage: string = error instanceof Error 
    ? error.message 
    : 'Network error occurred';
  const causeError: Error | undefined = error instanceof Error ? error : undefined;
  throw new Error(`Failed to download PDF: ${errorMessage}`, { cause: causeError });
}
