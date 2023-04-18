using System;

using Vec3 = System.Numerics.Vector3;

namespace Epsylon.TextureSquish
{
    class RangeFit : ColourFit
    {
        private static readonly Vec3 GRID = new Vec3(31.0f, 63.0f, 31.0f);
        private static readonly Vec3 GRIDRCP = Vec3.One / GRID;
        private static readonly Vec3 HALF = new Vec3(0.5f);

        public RangeFit(ColourSet colours, CompressionOptions flags) : base(colours)
        {
            // initialise the metric
            bool perceptual = ((flags & CompressionOptions.ColourMetricPerceptual) != 0);
            
            m_metric = perceptual ? new Vec3(0.2126f, 0.7152f, 0.0722f) : Vec3.One;

            // initialise the best error
            m_besterror = float.MaxValue;

            // cache some values
            var count = m_colours.Count;
            var values = m_colours.Points;
            var weights = m_colours.Weights;

            // get the covariance matrix
            Sym3x3 covariance = Sym3x3.ComputeWeightedCovariance(count, values, weights);

            // compute the principle component
            Vec3 principle = Sym3x3.ComputePrincipleComponent(covariance);

            // get the min and max range as the codebook endpoints
            Vec3 start = Vec3.Zero;
            Vec3 end = Vec3.Zero;

            if (count > 0)
            {
                float min, max;

                // compute the range
                start = end = values[0];
                min = max = Vec3.Dot(values[0], principle);
                for (int i = 1; i < count; ++i)
                {
                    float val = Vec3.Dot(values[i], principle);
                    if (val < min)
                    {
                        start = values[i];
                        min = val;
                    }
                    else if (val > max)
                    {
                        end = values[i];
                        max = val;
                    }
                }
            }

            // clamp the output to [0, 1]
            start = start.Clamp(Vec3.Zero, Vec3.One);
            end   =   end.Clamp(Vec3.Zero, Vec3.One);

            // clamp to the grid and save
            m_start = (GRID * start + HALF).Truncate() * GRIDRCP;
            m_end =   (GRID *   end + HALF).Truncate() * GRIDRCP;
        }

        protected override void Compress3(BlockWindow block)
        {
            // cache some values
            var count = m_colours.Count;
            var values = m_colours.Points;

            // create a codebook
            var codes = new Vec3[3];
            codes[0] = m_start;
            codes[1] = m_end;
            codes[2] = 0.5f * m_start + 0.5f * m_end;

            // match each point to the closest code
            var closest = new byte[16];
            float error = 0.0f;
            for (int i = 0; i < count; ++i)
            {
                // find the closest code
                float dist = float.MaxValue;
                int idx = 0;
                for (int j = 0; j < 3; ++j)
                {
                    float d = (m_metric * (values[i] - codes[j])).LengthSquared();
                    if (d < dist)
                    {
                        dist = d;
                        idx = j;
                    }
                }

                // save the index
                closest[i] = (byte) idx;

                // accumulate the error
                error += dist;
            }

            // save this scheme if it wins
            if (error < m_besterror)
            {
                // remap the indices
                var indices = new byte[16];
                m_colours.RemapIndices(closest, indices);

                // save the block
                block.WriteColourBlock3(m_start, m_end, indices);

                // save the error
                m_besterror = error;
            }
        }

        protected override void Compress4(BlockWindow block)
        {
            // cache some values
            var count = m_colours.Count;
            var values = m_colours.Points;

            // create a codebook
            var codes = new Vec3[4];
            codes[0] = m_start;
            codes[1] = m_end;
            codes[2] = (2.0f / 3.0f) * m_start + (1.0f / 3.0f) * m_end;
            codes[3] = (1.0f / 3.0f) * m_start + (2.0f / 3.0f) * m_end;

            // match each point to the closest code
            var closest = new byte[16];
            float error = 0.0f;
            for (int i = 0; i < count; ++i)
            {
                // find the closest code
                float dist = float.MaxValue;
                int idx = 0;
                for (int j = 0; j < 4; ++j)
                {
                    float d = (m_metric * (values[i] - codes[j])).LengthSquared();
                    if (d < dist)
                    {
                        dist = d;
                        idx = j;
                    }
                }

                // save the index
                closest[i] = (byte) idx;

                // accumulate the error
                error += dist;
            }

            // save this scheme if it wins
            if (error < m_besterror)
            {
                // remap the indices
                var indices = new byte[16];
                m_colours.RemapIndices(closest, indices);

                // save the block
                block.WriteColourBlock4(m_start, m_end, indices);

                // save the error
                m_besterror = error;
            }
        }

        private readonly Vec3 m_metric;
        private readonly Vec3 m_start;
        private readonly Vec3 m_end;

        private float m_besterror;
    };

}


