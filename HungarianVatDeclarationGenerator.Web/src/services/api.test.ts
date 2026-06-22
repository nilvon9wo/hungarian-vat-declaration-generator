import { describe, it, expect, beforeEach, vi } from 'vitest';
import { uploadCsvFile, downloadPdf } from '../services/api';
import type { VatDeclarationResult } from '../types/api';

// Hungarian VAT rate as defined by Hungarian tax law
const STANDARD_HUNGARIAN_VAT_RATE: number = 27;

// File and content constants
const SAMPLE_CSV_FILENAME: string = 'test.csv';
const TEXT_CSV_MIME_TYPE: string = 'text/csv';
const APPLICATION_PDF_MIME_TYPE: string = 'application/pdf';

const TEST_FILE_CONTENT: string = 'test';
const PDF_CONTENT: string = 'pdf content';

// HTTP status codes and messages
const HTTP_STATUS_BAD_REQUEST: number = 400;
const HTTP_STATUS_TEXT_BAD_REQUEST: string = 'Bad Request';

// Error messages
const SAMPLE_ERROR_MESSAGE: string = 'Invalid CSV format';
const NETWORK_ERROR_MESSAGE: string = 'Network error';

// Mock testing constants
const FORM_DATA_FILE_FIELD_NAME: string = 'file';
const MOCK_CALL_INDEX_FIRST: number = 0;
const MOCK_CALL_BODY_ARGUMENT_INDEX: number = 1;

describe('API Service', () => {
  beforeEach(() => {
    global.fetch = vi.fn();
  });

  describe('uploadCsvFile', () => {
    it('should successfully upload a file and return result', async () => {
      // Arrange
      const netAmount: number = 10000;
      const mockResult: VatDeclarationResult = {
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
      (global.fetch as ReturnType<typeof vi.fn>).mockResolvedValueOnce({
        ok: true,
        json: async () => mockResult,
      });
      const file: File = new File(['test'], 'test.csv', { type: 'text/csv' });

      // Act
      const result = await uploadCsvFile(file);

      // Assert
      expect(result.success).toBe(true);
      expect(result.data).toEqual(mockResult);
      expect(result.error).toBeUndefined();
    });

    it('should handle HTTP error responses', async () => {
      // Arrange
      (global.fetch as ReturnType<typeof vi.fn>).mockResolvedValueOnce({
        ok: false,
        status: HTTP_STATUS_BAD_REQUEST,
        statusText: HTTP_STATUS_TEXT_BAD_REQUEST,
        json: async () => ({ error: SAMPLE_ERROR_MESSAGE }),
      });
      const file: File = new File([TEST_FILE_CONTENT], SAMPLE_CSV_FILENAME, { type: TEXT_CSV_MIME_TYPE });

      // Act
      const result = await uploadCsvFile(file);

      // Assert
      expect(result.success).toBe(false);
      expect(result.error).toBe(SAMPLE_ERROR_MESSAGE);
    });

    it('should handle network errors', async () => {
      // Arrange
      (global.fetch as ReturnType<typeof vi.fn>).mockRejectedValueOnce(
        new Error(NETWORK_ERROR_MESSAGE)
      );
      const file: File = new File([TEST_FILE_CONTENT], SAMPLE_CSV_FILENAME, { type: TEXT_CSV_MIME_TYPE });

      // Act
      const result = await uploadCsvFile(file);

      // Assert
      expect(result.success).toBe(false);
      expect(result.error).toContain(NETWORK_ERROR_MESSAGE);
    });

    it('should send FormData with correct field name', async () => {
      // Arrange
      (global.fetch as ReturnType<typeof vi.fn>).mockResolvedValueOnce({
        ok: true,
        json: async () => ({}),
      });
      const file: File = new File([TEST_FILE_CONTENT], SAMPLE_CSV_FILENAME, { type: TEXT_CSV_MIME_TYPE });

      // Act
      await uploadCsvFile(file);

      // Assert
      const fetchCall = (global.fetch as ReturnType<typeof vi.fn>).mock.calls[MOCK_CALL_INDEX_FIRST];
      const formData: FormData = fetchCall[MOCK_CALL_BODY_ARGUMENT_INDEX].body as FormData;
      expect(formData.get(FORM_DATA_FILE_FIELD_NAME)).toBe(file);
    });
  });

  describe('downloadPdf', () => {
    it('should successfully download PDF as blob', async () => {
      // Arrange
      const mockBlob: Blob = new Blob([PDF_CONTENT], { type: APPLICATION_PDF_MIME_TYPE });
      (global.fetch as ReturnType<typeof vi.fn>).mockResolvedValueOnce({
        ok: true,
        blob: async () => mockBlob,
      });
      const file: File = new File([TEST_FILE_CONTENT], SAMPLE_CSV_FILENAME, { type: TEXT_CSV_MIME_TYPE });

      // Act
      const result: Blob = await downloadPdf(file) as Blob;

      // Assert
      expect(result).toBe(mockBlob);
    });

    it('should throw error on HTTP error response', async () => {
      // Arrange
      (global.fetch as ReturnType<typeof vi.fn>).mockResolvedValueOnce({
        ok: false,
        status: HTTP_STATUS_BAD_REQUEST,
        statusText: HTTP_STATUS_TEXT_BAD_REQUEST,
        json: async () => ({ error: SAMPLE_ERROR_MESSAGE }),
      });
      const file: File = new File([TEST_FILE_CONTENT], SAMPLE_CSV_FILENAME, { type: TEXT_CSV_MIME_TYPE });

      // Act & Assert
      await expect(downloadPdf(file)).rejects.toThrow(SAMPLE_ERROR_MESSAGE);
    });

    it('should throw error on network failure', async () => {
      // Arrange
      (global.fetch as ReturnType<typeof vi.fn>).mockRejectedValueOnce(
        new Error(NETWORK_ERROR_MESSAGE)
      );
      const file: File = new File([TEST_FILE_CONTENT], SAMPLE_CSV_FILENAME, { type: TEXT_CSV_MIME_TYPE });

      // Act & Assert
      await expect(downloadPdf(file)).rejects.toThrow(NETWORK_ERROR_MESSAGE);
    });
  });
});

function calculateVat(netAmount: number, vatRate: number): number {
  return netAmount * vatRate / 100;
}

function calculateGross(netAmount: number, vatRate: number): number {
  return netAmount + calculateVat(netAmount, vatRate);
}
