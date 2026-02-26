import type { CropArea } from 'svelte-easy-crop';

/**
 * Extract the cropped region from an image and return a JPEG blob
 * scaled to `outputSize` × `outputSize` pixels.
 */
export async function getCroppedBlob(
	imageSrc: string,
	pixelCrop: CropArea,
	outputSize = 512
): Promise<Blob> {
	const image = await loadImage(imageSrc);

	const canvas = document.createElement('canvas');
	canvas.width = outputSize;
	canvas.height = outputSize;

	const ctx = canvas.getContext('2d');
	if (!ctx) throw new Error('Canvas 2D context unavailable');

	ctx.drawImage(
		image,
		pixelCrop.x,
		pixelCrop.y,
		pixelCrop.width,
		pixelCrop.height,
		0,
		0,
		outputSize,
		outputSize
	);

	return new Promise<Blob>((resolve, reject) => {
		canvas.toBlob(
			(blob) => (blob ? resolve(blob) : reject(new Error('Canvas toBlob returned null'))),
			'image/jpeg',
			0.9
		);
	});
}

function loadImage(src: string): Promise<HTMLImageElement> {
	return new Promise((resolve, reject) => {
		const img = new Image();
		img.onload = () => resolve(img);
		img.onerror = reject;
		img.src = src;
	});
}
