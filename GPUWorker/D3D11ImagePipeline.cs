namespace GPUWorker
{
    using Hexa.NET.D3D11;
    using Hexa.NET.DirectXTex;
    using Hexa.NET.DXGI;
    using Hexa.NET.KittyUI.D3D11;
    using Hexa.NET.KittyUI.Graphics.Imaging;
    using HexaGen.Runtime.COM;
    using System;
    using System.Runtime.Versioning;
    using System.Threading;
    using WorkerShared;
    using ID3D11Device = Hexa.NET.D3D11.ID3D11Device;

    [SupportedOSPlatform("windows")]
    public class D3D11ImagePipeline : ImagePipelineBase
    {
        private ComPtr<ID3D11Device5> device;

        public D3D11ImagePipeline(WorkerIPCClient client) : base(client)
        {
            TextureLoader.ConvertSRGBToRGB = false;
            D3D11Adapter.Init(null!, false);
            device = D3D11GraphicsDevice.Device;
        }

        public static Format SRGBToRGB(Format format)
        {
            return format switch
            {
                Format.R8G8B8A8UnormSrgb => Format.R8G8B8G8Unorm,
                Format.B8G8R8X8UnormSrgb => Format.B8G8R8X8Unorm,
                Format.B8G8R8A8UnormSrgb => Format.B8G8R8A8Unorm,
                _ => format
            };
        }

        protected override JobFinish ProcessImage(JobPayload workload, CancellationToken cancellationToken)
        {
            ImageSource? texture = null;
            try
            {
                texture = TextureLoader.LoadFormFile(workload.Source!);

                var metadata = texture.Metadata;

                if (DirectXTex.IsSRGB(metadata.Format))
                {
                    Format format = SRGBToRGB((Format)metadata.Format);
                    SwapImage(ref texture, texture.Convert(format, TexFilterFlags.Default));
                }

                var isQuad = metadata.Width == metadata.Height;
                if ((workload.Flags & WorkloadFlags.Upscale) != 0 && isQuad)
                {
                    if ((long)metadata.Width < workload.MinSize)
                    {
                        SwapImage(ref texture, texture.Resize(workload.MinSize, workload.MinSize, TexFilterFlags.Cubic));
                    }
                }

                if ((workload.Flags & WorkloadFlags.Downscale) != 0 && isQuad)
                {
                    if ((long)metadata.Width > workload.MaxSize)
                    {
                        SwapImage(ref texture, texture.Resize(workload.MaxSize, workload.MaxSize, TexFilterFlags.Cubic));
                    }
                }

                if ((workload.Flags & WorkloadFlags.FlipVertical) != 0)
                {
                    SwapImage(ref texture, texture.FlipRotate(TexFRFlags.FlipVertical));
                }

                if ((workload.Flags & WorkloadFlags.GenerateMips) != 0)
                {
                    SwapImage(ref texture, texture.GenerateMipMaps(TexFilterFlags.Default));
                }

                TexCompressFlags compressFlags = 0;

                if ((workload.Flags & WorkloadFlags.Bc7Quick) != 0)
                {
                    compressFlags |= TexCompressFlags.Bc7Quick;
                }

                unsafe
                {
                    if (DirectXTex.IsCompressed(workload.Format))
                    {
                        SwapImage(ref texture, texture.Compress(device.As<ID3D11Device>(), (Format)workload.Format, compressFlags));
                    }
                    else
                    {
                        SwapImage(ref texture, texture.Convert((Format)workload.Format, TexFilterFlags.Default));
                    }
                }

                texture.SaveToFile(workload.Destination!, TexFileFormat.DDS, (int)(DDSFlags.ForceDx10Ext | DDSFlags.BadDxtnTails));
                return new JobFinish(workload.Id, resultCode: 0, $"[{workload.Source}] Processed -> {workload.Destination}");
            }
            catch (Exception ex)
            {
                return new JobFinish(workload.Id, resultCode: -1, $"[{workload.Source}] {ex.Message}");
            }
            finally
            {
                texture?.Dispose();
            }
        }

        private static void SwapImage(ref ImageSource before, ImageSource after)
        {
            before.Dispose();
            before = after;
        }

        protected override void DisposeCore()
        {
            base.DisposeCore();
            D3D11Adapter.Shutdown();
        }
    }
}