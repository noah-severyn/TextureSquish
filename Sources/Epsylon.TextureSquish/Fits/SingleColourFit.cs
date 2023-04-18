using System;

using Vec3 = System.Numerics.Vector3;
using Vec4 = System.Numerics.Vector4;


namespace Epsylon.TextureSquish
{
    class SingleColourFit : ColourFit
    {
        private static readonly SingleColourLookup[][] LOOKUPTABLES3 =
            new SingleColourLookup[][]
            {
                SingleColourLookupTables.lookup_5_3,
                SingleColourLookupTables.lookup_6_3,
                SingleColourLookupTables.lookup_5_3
            };

        // build the table of lookups
        private static readonly SingleColourLookup[][] LOOKUPTABLES4 =
            new SingleColourLookup[][]
            {
                SingleColourLookupTables.lookup_5_4,
                SingleColourLookupTables.lookup_6_4,
                SingleColourLookupTables.lookup_5_4
            };

        public SingleColourFit(ColourSet colours, CompressionOptions flags) : base(colours)
        {
            // grab the single colour
            var values = m_colours.Points;
            m_colour[0] = (byte) ((255.0f * values[0].X).FloatToInt(255));
            m_colour[1] = (byte) ((255.0f * values[0].Y).FloatToInt(255));
            m_colour[2] = (byte) ((255.0f * values[0].Z).FloatToInt(255));

            // initialise the best error
            m_besterror = int.MaxValue;
        }

        protected override void Compress3(BlockWindow block)
        {
            // find the best end-points and index
            ComputeEndPoints(LOOKUPTABLES3);

            // build the block if we win
            if (m_error < m_besterror)
            {
                // remap the indices
                var indices = new byte[16];
                m_colours.RemapIndices(new byte[] { m_index }, indices);

                // save the block
                block.WriteColourBlock3(m_start, m_end, indices);

                // save the error
                m_besterror = m_error;
            }
        }

        protected override void Compress4(BlockWindow block)
        {
            // find the best end-points and index
            ComputeEndPoints(LOOKUPTABLES4);

            // build the block if we win
            if (m_error < m_besterror)
            {
                // remap the indices
                var indices = new byte[16];
                m_colours.RemapIndices(new byte[] { m_index }, indices);

                // save the block
                block.WriteColourBlock4(m_start, m_end, indices);

                // save the error
                m_besterror = m_error;
            }
        }

        private void ComputeEndPoints(SingleColourLookup[][] lookups)
        {
            // check each index combination (endpoint or intermediate)
            m_error = int.MaxValue;
            for (int index = 0; index < 2; ++index)
            {
                // check the error for this codebook index
                var sources = new SourceBlock[3];
                int error = 0;
                for (int channel = 0; channel < 3; ++channel)
                {
                    // grab the lookup table and index for this channel
                    var lookup = lookups[channel];
                    int target = m_colour[channel];

                    // store a pointer to the source for this channel
                    sources[channel] = lookup[target].sources[index];

                    // accumulate the error
                    int diff = sources[channel].error;
                    error += diff * diff;
                }

                // keep it if the error is lower
                if (error < m_error)
                {
                    m_start = new Vec3(
                        (float)sources[0].start / 31.0f,
                        (float)sources[1].start / 63.0f,
                        (float)sources[2].start / 31.0f
                    );
                    m_end = new Vec3(
                        (float)sources[0].end / 31.0f,
                        (float)sources[1].end / 63.0f,
                        (float)sources[2].end / 31.0f
                    );
                    m_index = (byte) (2 * index);
                    m_error = error;
                }
            }
        }

        byte[] m_colour = new byte[3];
        Vec3 m_start;
        Vec3 m_end;
        byte m_index;
        int m_error;
        int m_besterror;
    }

}


