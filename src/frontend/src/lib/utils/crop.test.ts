/**
 * Tests for the canvas-based crop utility.
 *
 * Runs in node environment — DOM APIs (document, Image) are stubbed globally.
 */
import { describe, expect, it, vi, beforeEach } from 'vitest';

const mockDrawImage = vi.fn();
const mockToBlob = vi.fn();
const mockGetContext = vi.fn();

let capturedCanvas: { width: number; height: number };

vi.stubGlobal('document', {
	createElement: vi.fn((tag: string) => {
		if (tag === 'canvas') {
			capturedCanvas = {
				width: 0,
				height: 0,
				getContext: mockGetContext,
				toBlob: mockToBlob
			} as unknown as { width: number; height: number };
			return capturedCanvas;
		}
		throw new Error(`Unexpected createElement("${tag}")`);
	})
});

vi.stubGlobal(
	'Image',
	vi.fn(() => {
		const img = { onload: null as (() => void) | null, onerror: null, src: '' };
		setTimeout(() => img.onload?.(), 0);
		return img;
	})
);

const { getCroppedBlob } = await import('./crop');

describe('getCroppedBlob', () => {
	const crop = { x: 10, y: 20, width: 100, height: 100 };

	beforeEach(() => {
		mockDrawImage.mockClear();
		mockToBlob.mockClear();
		mockGetContext.mockReset();
		mockGetContext.mockReturnValue({ drawImage: mockDrawImage });
		mockToBlob.mockImplementation((cb: BlobCallback) =>
			cb(new Blob(['fake'], { type: 'image/jpeg' }))
		);
	});

	it('returns a JPEG blob', async () => {
		const blob = await getCroppedBlob('data:image/png;base64,fake', crop);
		expect(blob).toBeInstanceOf(Blob);
		expect(blob.type).toBe('image/jpeg');
	});

	it('sets canvas dimensions to the specified outputSize', async () => {
		await getCroppedBlob('data:image/png;base64,fake', crop, 256);
		expect(capturedCanvas.width).toBe(256);
		expect(capturedCanvas.height).toBe(256);
	});

	it('defaults outputSize to 512', async () => {
		await getCroppedBlob('data:image/png;base64,fake', crop);
		expect(capturedCanvas.width).toBe(512);
		expect(capturedCanvas.height).toBe(512);
	});

	it('calls drawImage with correct crop coordinates', async () => {
		await getCroppedBlob('data:image/png;base64,fake', crop, 512);
		expect(mockDrawImage).toHaveBeenCalledWith(
			expect.any(Object),
			10,
			20,
			100,
			100,
			0,
			0,
			512,
			512
		);
	});

	it('requests JPEG at 0.9 quality', async () => {
		await getCroppedBlob('data:image/png;base64,fake', crop);
		expect(mockToBlob).toHaveBeenCalledWith(expect.any(Function), 'image/jpeg', 0.9);
	});

	it('throws when canvas 2D context is unavailable', async () => {
		mockGetContext.mockReturnValue(null);
		await expect(getCroppedBlob('data:image/png;base64,fake', crop)).rejects.toThrow(
			'Canvas 2D context unavailable'
		);
	});

	it('throws when toBlob returns null', async () => {
		mockToBlob.mockImplementation((cb: BlobCallback) => cb(null));
		await expect(getCroppedBlob('data:image/png;base64,fake', crop)).rejects.toThrow(
			'Canvas toBlob returned null'
		);
	});
});
