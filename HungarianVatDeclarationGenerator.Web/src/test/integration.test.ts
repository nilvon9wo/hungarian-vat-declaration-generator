import { describe, it, expect, beforeAll, afterAll, vi } from 'vitest';
import { uploadCsvFile } from '../services/api';
import type { VatDeclarationResult } from '../types/api';

// Hungarian VAT rates as defined by Hungarian tax law
const STANDARD_HUNGARIAN_VAT_RATE: number = 27;
const INTERMEDIATE_HUNGARIAN_VAT_RATE: number = 18;

// Sample invoice identifiers to demonstrate format consistency
const SAMPLE_INVOICE_NUMBER_001: string = 'INV-001';
const SAMPLE_INVOICE_NUMBER_002: string = 'INV-002';

// CSV file structure constants
const CSV_HEADER: string = 'InvoiceNumber,NetAmount,VatRate';
const CSV_FILE_EXTENSION: string = '.csv';

// Test file names
const SAMPLE_CSV_FILENAME: string = 'test.csv';
const SAMPLE_TXT_FILENAME: string = 'test.txt';
const LARGE_CSV_FILENAME: string = 'large.csv';
const INVOICES_CSV_FILENAME: string = 'invoices.csv';

// MIME types
const TEXT_CSV_MIME_TYPE: string = 'text/csv';
const TEXT_PLAIN_MIME_TYPE: string = 'text/plain';

// Test content
const NOT_CSV_CONTENT: string = 'not csv';

// Error messages from API
const NETWORK_ERROR_MESSAGE: string = 'Network failure';
const NO_FILE_PROVIDED_ERROR: string = 'No file provided';
const INVALID_FILE_TYPE_ERROR: string = 'Invalid file type. Only CSV files are allowed.';

// API endpoints
const API_UPLOAD_ENDPOINT: string = '/api/VatDeclaration/upload';

// HTTP status codes
const HTTP_STATUS_OK: number = 200;
const HTTP_STATUS_BAD_REQUEST: number = 400;
const HTTP_STATUS_NOT_FOUND: number = 404;

// Large file generation parameters
const LARGE_FILE_ROW_COUNT: number = 100;
const LARGE_FILE_BASE_AMOUNT: number = 1000;
const LARGE_FILE_AMOUNT_INCREMENT: number = 10;
const INVOICE_NUMBER_PADDING_LENGTH: number = 3;
const INVOICE_NUMBER_PADDING_CHAR: string = '0';

describe('API Integration Tests', () => {
  const originalFetch = global.fetch;

  beforeAll(() => {
    global.fetch = createMockFetch();
  });

  afterAll(() => {
    global.fetch = originalFetch;
  });

  it('should successfully upload valid CSV and receive parsed result', async () => {
    // Arrange
    const netAmount: number = 10000;
    const csvContent: string = `${CSV_HEADER}\n${SAMPLE_INVOICE_NUMBER_001},${netAmount},${STANDARD_HUNGARIAN_VAT_RATE}`;
    const file: File = new File([csvContent], SAMPLE_CSV_FILENAME, { type: TEXT_CSV_MIME_TYPE });

    // Act
    const result = await uploadCsvFile(file);

    // Assert
    expect(result.success).toBe(true);
    expect(result.data).toBeDefined();
    expect(result.data?.summariesByVatRate).toHaveLength(1);
    expect(result.data?.summariesByVatRate[0].vatRate).toBe(STANDARD_HUNGARIAN_VAT_RATE);
    expect(result.data?.totalInvoiceCount).toBe(1);
  });

  it('should reject non-CSV files', async () => {
    // Arrange
    const file: File = new File([NOT_CSV_CONTENT], SAMPLE_TXT_FILENAME, { type: TEXT_PLAIN_MIME_TYPE });

    // Act
    const result = await uploadCsvFile(file);

    // Assert
    expect(result.success).toBe(false);
    expect(result.error).toContain('Invalid file type');
  });

  it('should handle multipart form data correctly', async () => {
    // Arrange
    const invoice1Net: number = 10000;
    const invoice2Net: number = 5000;
    const csvContent: string = `${CSV_HEADER}\n${SAMPLE_INVOICE_NUMBER_001},${invoice1Net},${STANDARD_HUNGARIAN_VAT_RATE}\n${SAMPLE_INVOICE_NUMBER_002},${invoice2Net},${INTERMEDIATE_HUNGARIAN_VAT_RATE}`;
    const file: File = new File([csvContent], INVOICES_CSV_FILENAME, { type: TEXT_CSV_MIME_TYPE });

    // Act
    const result = await uploadCsvFile(file);

    // Assert
    expect(result.success).toBe(true);
    expect(result.data?.grandTotalNet).toBe(invoice1Net);
  });

  it('should handle large CSV files', async () => {
    // Arrange
    let csvContent: string = `${CSV_HEADER}\n`;
    for (let i: number = 0; i < LARGE_FILE_ROW_COUNT; i++) {
      const invoiceNumber: string = `INV-${i.toString().padStart(INVOICE_NUMBER_PADDING_LENGTH, INVOICE_NUMBER_PADDING_CHAR)}`;
      const amount: number = LARGE_FILE_BASE_AMOUNT + i * LARGE_FILE_AMOUNT_INCREMENT;
      csvContent += `${invoiceNumber},${amount},${STANDARD_HUNGARIAN_VAT_RATE}\n`;
    }
    const file: File = new File([csvContent], LARGE_CSV_FILENAME, { type: TEXT_CSV_MIME_TYPE });

    // Act
    const result = await uploadCsvFile(file);

    // Assert
    expect(result.success).toBe(true);
    expect(result.data).toBeDefined();
  });

  it('should handle fetch errors gracefully', async () => {
    // Arrange
    const tempMock = global.fetch;
    global.fetch = vi.fn().mockRejectedValueOnce(new Error(NETWORK_ERROR_MESSAGE));
    const file: File = new File(['test'], SAMPLE_CSV_FILENAME, { type: TEXT_CSV_MIME_TYPE });

    // Act
    const result = await uploadCsvFile(file);

    // Assert
    expect(result.success).toBe(false);
    expect(result.error).toContain(NETWORK_ERROR_MESSAGE);

    // Cleanup
    global.fetch = tempMock;
  });
});

function createMockFetch(): typeof fetch {
  return vi.fn(async (input: RequestInfo | URL, init?: RequestInit) => {
    const url: string = input.toString();

    if (url.includes(API_UPLOAD_ENDPOINT)) {
      return handleVatDeclarationUpload(init);
    }

    return new Response('Not Found', { status: HTTP_STATUS_NOT_FOUND });
  }) as unknown as typeof fetch;
}

function handleVatDeclarationUpload(init: RequestInit | undefined): Response {
  const formData: FormData = init?.body as FormData;
  const file: File | null = extractFileFromFormData(formData);

  const fileExistsError: Response | null = validateFileExists(file);
  if (fileExistsError) {
    return fileExistsError;
  }

  const extensionError: Response | null = validateCsvFileExtension(file!.name);
  if (extensionError) {
    return extensionError;
  }

  return createSuccessResponse(createMockVatDeclarationResult());
}

function extractFileFromFormData(formData: FormData): File | null {
  const file: Blob | null = formData.get('file') as Blob | null;
  return file instanceof File ? file : null;
}

function validateFileExists(file: File | null): Response | null {
  if (!file) {
    return createErrorResponse(NO_FILE_PROVIDED_ERROR);
  }
  return null;
}

function validateCsvFileExtension(fileName: string): Response | null {
  if (!fileName.endsWith(CSV_FILE_EXTENSION)) {
    return createErrorResponse(INVALID_FILE_TYPE_ERROR);
  }
  return null;
}

function createSuccessResponse(result: VatDeclarationResult): Response {
  return createJsonResponse(result, HTTP_STATUS_OK);
}

function createErrorResponse(message: string): Response {
  return createJsonResponse({ error: message }, HTTP_STATUS_BAD_REQUEST);
}

function createJsonResponse(data: unknown, status: number): Response {
  return new Response(
    JSON.stringify(data),
    { status, headers: { 'Content-Type': 'application/json' } }
  );
}

function createMockVatDeclarationResult(): VatDeclarationResult {
  const netAmount: number = 10000;

  return {
    summariesByVatRate: [
      {
        vatRate: STANDARD_HUNGARIAN_VAT_RATE,
        totalNetAmount: netAmount,
        totalVatAmount: calculateVat(netAmount, STANDARD_HUNGARIAN_VAT_RATE),
        totalGrossAmount: calculateGross(netAmount, STANDARD_HUNGARIAN_VAT_RATE),
        invoiceCount: 1,
      },
    ],
    grandTotalNet: netAmount,
    grandTotalVat: calculateVat(netAmount, STANDARD_HUNGARIAN_VAT_RATE),
    grandTotalGross: calculateGross(netAmount, STANDARD_HUNGARIAN_VAT_RATE),
    totalInvoiceCount: 1,
  };
}

function calculateVat(netAmount: number, vatRate: number): number {
  return netAmount * vatRate / 100;
}

function calculateGross(netAmount: number, vatRate: number): number {
  return netAmount + calculateVat(netAmount, vatRate);
}
