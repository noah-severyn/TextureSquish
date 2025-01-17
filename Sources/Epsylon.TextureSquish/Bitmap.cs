﻿using System;
using System.Collections.Generic;
using System.Text;

namespace Epsylon.TextureSquish {
    public class Bitmap {
        public Bitmap(int width, int height) {
            if (width <= 0) throw new ArgumentOutOfRangeException(nameof(width));
            if (height <= 0) throw new ArgumentOutOfRangeException(nameof(height));

            _Width = width;
            _Height = height;
            _Data = new byte[_Width * _Height * 4];
        }

        public Bitmap(byte[] rgba, int width, int height, bool isPreMultiplied = false) {
            if (rgba == null) throw new ArgumentNullException(nameof(rgba));
            if (width <= 0) throw new ArgumentOutOfRangeException(nameof(width));
            if (height <= 0) throw new ArgumentOutOfRangeException(nameof(height));
            if (rgba.Length < (width * height * 4)) throw new ArgumentException(nameof(rgba));

            _Data = rgba;
            _IsPreMultiplied = isPreMultiplied;
            _Width = width;
            _Height = height;
        }

        public Bitmap Clone() {
            return new Bitmap((byte[]) _Data.Clone(), _Width, _Height) {
                _IsPreMultiplied = this._IsPreMultiplied
            };
        }

        private readonly byte[] _Data;
        private readonly int _Width;
        private readonly int _Height;

        private bool _IsPreMultiplied;

        public int Width => _Width;
        public int Height => _Height;


        /// <summary>
        /// True if the current bitmap has alpha premultiplied colors.
        /// </summary>
        public bool IsPreMultiplied => _IsPreMultiplied;

        public byte[] Data => _Data;

        public uint this[int x, int y] {
            get {
                var idx = (y * _Width + x) * 4;

                uint value = 0;

                value |= _Data[idx + 0];
                value |= (uint) _Data[idx + 1] << 8;
                value |= (uint) _Data[idx + 2] << 16;
                value |= (uint) _Data[idx + 3] << 24;

                return value;
            }
            set {
                var idx = (y * _Width + x) * 4;

                _Data[idx + 0] = (byte) (value & 255);
                _Data[idx + 1] = (byte) ((value >> 8) & 255);
                _Data[idx + 2] = (byte) ((value >> 16) & 255);
                _Data[idx + 3] = (byte) ((value >> 24) & 255);
            }
        }

        internal void CopyBlockTo(int x, int y, byte[] block, out int mask) {
            mask = 0;

            int targetPixelIdx = 0;

            for (int py = 0; py < 4; ++py) {
                for (int px = 0; px < 4; ++px) {
                    // get the source pixel in the image
                    int sx = x + px;
                    int sy = y + py;

                    // enable if we're in the image
                    if (sx < _Width && sy < _Height) {
                        // copy the rgba value
                        int sourcePixelIdx = 4 * (_Width * sy + sx);

                        for (int i = 0; i < 4; ++i) block[targetPixelIdx++] = _Data[sourcePixelIdx++];

                        // enable this pixel
                        mask |= (1 << (4 * py + px));
                    } else {
                        // skip this pixel as its outside the image
                        targetPixelIdx += 4;
                    }
                }
            }
        }

        internal void SetBlock(int x, int y, byte[] block) {
            int sourcePixelIdx = 0;

            for (int py = 0; py < 4; ++py) {
                for (int px = 0; px < 4; ++px) {
                    // get the source pixel in the image
                    int sx = x + px;
                    int sy = y + py;

                    // enable if we're in the image
                    if (sx >= _Width || sy >= _Height) continue;

                    // copy the rgba value
                    int targetPixelIdx = 4 * (_Width * sy + sx);

                    for (int i = 0; i < 4; ++i) _Data[targetPixelIdx++] = block[sourcePixelIdx++];
                }
            }
        }

        
        public void SwapElements(int r, int g, int b, int a) {
            var tmp = new byte[4];

            for (var i = 0; i < _Data.Length; i += 4) {
                tmp[0] = _Data[i + 0];
                tmp[1] = _Data[i + 1];
                tmp[2] = _Data[i + 2];
                tmp[3] = _Data[i + 3];

                _Data[i + 0] = tmp[r];
                _Data[i + 1] = tmp[g];
                _Data[i + 2] = tmp[b];
                _Data[i + 3] = tmp[a];
            }
        }

        public void PremultiplyAlpha() {
            for (var i = 0; i < _Data.Length; i += 4) {
                var r = (int) _Data[i + 0];
                var g = (int) _Data[i + 1];
                var b = (int) _Data[i + 2];
                var a = (int) _Data[i + 3];

                _Data[i + 0] = (byte) ((r * a) / 255);
                _Data[i + 1] = (byte) ((g * a) / 255);
                _Data[i + 2] = (byte) ((b * a) / 255);
            }

            _IsPreMultiplied = true;
        }


        /// <summary>
        /// Decompresses an image in memory.
        /// </summary>
        /// <remarks>
        /// The decompressed pixels will be written as a contiguous array of width*height
        /// 16 rgba values, with each component as 1 byte each. In memory this is:
        /// 
        ///    { r1, g1, b1, a1, .... , rn, gn, bn, an } for n = width*height
        ///    
        /// The flags parameter should specify either kDxt1, kDxt3 or kDxt5 compression, 
        /// however, DXT1 will be used by default if none is specified. All other flags 
        /// Internally this function calls squish::Decompress for each block.
        /// </remarks>
        /// <param name="width">The width of the image.</param>
        /// <param name="height">The height of the image.</param>
        /// <param name="blocks">The compressed DXT blocks.</param>
        /// <param name="flags">Compression flags.</param>
        public static Bitmap Decompress(int width, int height, byte[] blocks, CompressionMode mode) {
            var img = new Bitmap(new byte[width * height * 4], width, height);

            DecompressImage(img, blocks, mode);

            return img;
        }

        /// <summary>
        /// Compresses an image in memory.
        /// </summary>
        /// <param name="flags">Compression Flags</param>
        /// <returns>The compressed blocks</returns>
        /// <remarks>
        /// The source pixels should be presented as a contiguous array of width* height
        ///
        /// rgba values, with each component as 1 byte each.In memory this should be:
        ///   
        ///       { r1, g1, b1, a1, .... , rn, gn, bn, an } for n = width* height
        ///
        /// The flags parameter should specify either kDxt1, kDxt3 or kDxt5 compression,
        /// however, DXT1 will be used by default if none is specified.When using DXT1 
        /// compression, 8 bytes of storage are required for each compressed DXT block.
        /// DXT3 and DXT5 compression require 16 bytes of storage per block.
        ///
        /// The flags parameter can also specify a preferred colour compressor and
        /// colour error metric to use when fitting the RGB components of the data. 
        /// Possible colour compressors are: kColourClusterFit (the default), 
        /// kColourRangeFit or kColourIterativeClusterFit.Possible colour error metrics         
        /// are: kColourMetricPerceptual(the default) or kColourMetricUniform.If no
        /// flags are specified in any particular category then the default will be
        /// used.Unknown flags are ignored.
        /// 
        /// When using kColourClusterFit, an additional flag can be specified to
        /// weight the colour of each pixel by its alpha value.For images that are
        /// rendered using alpha blending, this can significantly increase the
        /// perceived quality.
        /// 
        /// Internally this function calls squish::Compress for each block. To see how
        /// much memory is required in the compressed image, use
        /// squish::GetStorageRequirements.
        /// </remarks>
        public byte[] Compress(CompressionMode mode, CompressionOptions options) {
            var l = GetStorageRequirements(_Width, _Height, mode);

            var blocks = new byte[l];

            CompressImage(this, blocks, mode, options);

            return blocks;
        }

        private static void CompressImage(Bitmap srcImage, byte[] blocks, CompressionMode mode, CompressionOptions options) {
            // fix any bad flags
            options = options.FixFlags();

            int block_width = (srcImage.Width + 3) / 4;
            int block_height = (srcImage.Height + 3) / 4;

            // if the number of chunks to process is not very large, we better skip parallel processing
            if (block_width * block_height < 16) options &= ~CompressionOptions.UseParallelProcessing;

            if ((options & CompressionOptions.UseParallelProcessing) != 0) {
                System.Threading.Tasks.Parallel.For
                    (
                    0,
                    block_height,
                    (y, state) => {
                        // initialise the block output
                        var block = new BlockWindow(blocks, mode);

                        block.Offset += block.ByteLength * y * block_width;

                        // build the 4x4 block of pixels
                        var sourceRgba = new byte[16 * 4];

                        for (int x = 0; x < block_width; x++) {
                            srcImage.CopyBlockTo(x * 4, y * 4, sourceRgba, out int mask);

                            // compress it into the output
                            block.CompressMasked(sourceRgba, mask, options);

                            // advance
                            block.Offset += block.ByteLength;
                        }
                    }
                    );
            } else {
                // initialise the block output
                var block = new BlockWindow(blocks, mode);

                // build the 4x4 block of pixels
                var sourceRgba = new byte[16 * 4];

                // loop over blocks
                for (int y = 0; y < block_height; ++y) {
                    for (int x = 0; x < block_width; ++x) {
                        srcImage.CopyBlockTo(x * 4, y * 4, sourceRgba, out int mask);

                        // compress it into the output
                        block.CompressMasked(sourceRgba, mask, options);

                        // advance
                        block.Offset += block.ByteLength;
                    }
                }
            }
        }

        private static void DecompressImage(Bitmap dstImage, byte[] blocks, CompressionMode mode) {
            // initialise the block input
            var block = new BlockWindow(blocks, mode);

            var targetRgba = new byte[4 * 16];

            // loop over blocks
            for (int y = 0; y < dstImage.Height; y += 4) {
                for (int x = 0; x < dstImage.Width; x += 4) {
                    // decompress the block
                    block.Decompress(targetRgba);

                    // write the decompressed pixels to the correct image locations
                    dstImage.SetBlock(x, y, targetRgba);

                    // advance
                    block.Offset += block.ByteLength;
                }
            }
        }

        /// <summary>
        /// Computes the amount of compressed storage required.
        /// </summary>
        /// <param name="width">The width of the image.</param>
        /// <param name="height">The height of the image.</param>
        /// <param name="flags">Compression flags.</param>
        /// <returns>The amount of bytes required</returns>
        /// <remarks>
        /// The flags parameter should specify either kDxt1, kDxt3 or kDxt5 compression, 
        /// however, DXT1 will be used by default if none is specified. All other flags
        /// are ignored.
        /// 
        /// Most DXT images will be a multiple of 4 in each dimension, but this 
        /// function supports arbitrary size images by allowing the outer blocks to
        /// be only partially used.
        /// </remarks>
        static int GetStorageRequirements(int width, int height, CompressionMode flags) {
            // compute the storage requirements
            int blockcount = ((width + 3) / 4) * ((height + 3) / 4);
            int blocksize = ((flags & CompressionMode.Dxt1) != 0) ? 8 : 16;
            return blockcount * blocksize;
        }
    }
}
