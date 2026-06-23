import type { VatDeclarationResult, ApiError, ClientConfig } from '../types/api';

const API_BASE_URL = '/api';
const REQUEST_TIMEOUT_MS = 60000;
const API_KEY = import.meta.env.VITE_API_KEY || '';

export interface UploadResult {
  success: boolean;
  data?: VatDeclarationResult;
  error?: string;
}

export async function fetchConfig(): Promise<ClientConfig> {
  const response: Response = await fetchWithTimeout(
    `${API_BASE_URL}/Config`,
    {
      method: 'GET',
      headers: createAuthHeaders(),
    },
    REQUEST_TIMEOUT_MS
  );

  if (!response.ok) {
    throw new Error('Failed to fetch configuration');
  }

  return await response.json();
}

export async function uploadCsvFile(file: File): Promise<UploadResult> {
  const formData: FormData = createFormData(file);

  try {
    const response: Response = await fetchWithTimeout(
      `${API_BASE_URL}/VatDeclaration/upload`,
      {
        method: 'POST',
        body: formData,
        headers: createAuthHeaders(),
      },
      REQUEST_TIMEOUT_MS
    );

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

function createAuthHeaders(): HeadersInit {
  return {
    'X-API-Key': API_KEY,
  };
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
  const response: Response = await fetchWithTimeout(
    `${API_BASE_URL}/VatDeclaration/upload-and-generate-pdf`,
    {
      method: 'POST',
      body: formData,
      headers: createAuthHeaders(),
    },
    REQUEST_TIMEOUT_MS
  );

  if (!response.ok) {
    const errorMessage: string = await handleErrorResponse(response);
    const error = new Error(errorMessage);
    throw new Error(`Failed to download PDF: ${errorMessage}`, { cause: error });
  }

  return await response.blob();
}

async function fetchWithTimeout(
  url: string,
  options: RequestInit,
  timeoutMs: number
): Promise<Response> {
  const controller = new AbortController();
  const timeoutId = setTimeout(() => controller.abort(), timeoutMs);

  try {
    const response = await fetch(url, {
      ...options,
      signal: controller.signal,
    });
    clearTimeout(timeoutId);
    return response;
  } catch (error) {
    clearTimeout(timeoutId);
    if (error instanceof Error && error.name === 'AbortError') {
      throw new Error('Request timeout - please try again with a smaller file', { cause: error });
    }
    throw error;
  }
}

function wrapPdfError(error: unknown): never {
  const errorMessage: string = error instanceof Error 
    ? error.message 
    : 'Network error occurred';
  const causeError: Error | undefined = error instanceof Error ? error : undefined;
  throw new Error(`Failed to download PDF: ${errorMessage}`, { cause: causeError });
}
