public class CompressedTrack
{
    public struct Header
    {
        public int numNotes;
        public int numUniqueSemitones;
        public int numUniqueDurations;
        public int bitsPerSemitone;
        public int bitsPerDuration;

        public void DumpToConsole()
        {
            Console.WriteLine("\n=========== Compressed Track Header ===========");
            Console.WriteLine($"Notes: {numNotes}");
            Console.WriteLine($"Unique Semitones: {numUniqueSemitones}");
            Console.WriteLine($"Unique Durations: {numUniqueDurations}");
            Console.WriteLine($"Bits Per Semitone: {bitsPerSemitone}");
            Console.WriteLine($"Bits Per Duration: {bitsPerDuration}");
        }
    }

    public Header header;

    public int[] semitoneFrequencyTable = Array.Empty<int>();
    public int[] durationMsTable = Array.Empty<int>();

    public int[] rawSemitoneIndices = Array.Empty<int>();
    public int[] rawDurationIndices = Array.Empty<int>();

    public byte[] compressedSemitones = Array.Empty<byte>();
    public byte[] compressedDurations = Array.Empty<byte>();

    public static CompressedTrack Build(Track track)
    {
        // Calculate all the unique semitones and durations in this track
        // So that we can start compressing it
        Dictionary<long, int> semitoneTable = new Dictionary<long, int>();
        Dictionary<long, int> durationTable = new Dictionary<long, int>();
        foreach (RawNote note in track.notes)
        {
            if (!semitoneTable.ContainsKey(note.semiTone))
            {
                semitoneTable[note.semiTone] = semitoneTable.Count;
            }

            if (!durationTable.ContainsKey(note.durationRaw))
            {
                durationTable[note.durationRaw] = durationTable.Count;
            }
        }

        // Build header for compressed track
        CompressedTrack compressedTrack = new CompressedTrack();
        compressedTrack.header.numNotes = track.notes.Count;
        compressedTrack.header.numUniqueSemitones = semitoneTable.Count;
        compressedTrack.header.numUniqueDurations = durationTable.Count;
        compressedTrack.header.bitsPerSemitone = (int)Math.Ceiling(Math.Log2(compressedTrack.header.numUniqueSemitones));
        compressedTrack.header.bitsPerDuration = (int)Math.Ceiling(Math.Log2(compressedTrack.header.numUniqueDurations));
        compressedTrack.header.DumpToConsole();

        // Write out semitone frequency mapping
        compressedTrack.semitoneFrequencyTable = new int[semitoneTable.Count];
        foreach ((long semitone, int index) in semitoneTable)
        {
            int frequency = 
                semitone == -1 
                ? 0
                : (int)Math.Round(440.0f * Math.Pow(2.0, (semitone - 49.0) /12.0));

            compressedTrack.semitoneFrequencyTable[index] = frequency;
        }

        // Write out duration ms mapping
        compressedTrack.durationMsTable = new int[durationTable.Count];
        foreach ((long duration, int index) in durationTable)
        {
            compressedTrack.durationMsTable[index] = (int)duration;
        }

        // Compress the semitones
        int numSemitoneBytes = (int)Math.Ceiling(compressedTrack.header.numNotes * compressedTrack.header.bitsPerSemitone / 8.0f);
        compressedTrack.rawSemitoneIndices = new int[compressedTrack.header.numNotes];
        compressedTrack.compressedSemitones = new byte[numSemitoneBytes];

        for (int i = 0; i < track.notes.Count; i++)
        {
            int semitoneIndex = semitoneTable[track.notes[i].semiTone];
            compressedTrack.rawSemitoneIndices[i] = semitoneIndex;

            int writeBitIndex = i * compressedTrack.header.bitsPerSemitone;
            CompressedWrite(compressedTrack.compressedSemitones, writeBitIndex, compressedTrack.header.bitsPerSemitone, semitoneIndex);
        }

        // Compress the durations
        int numDurationBytes = (int)Math.Ceiling(compressedTrack.header.numNotes * compressedTrack.header.bitsPerDuration / 8.0f);
        compressedTrack.rawDurationIndices = new int[compressedTrack.header.numNotes];
        compressedTrack.compressedDurations = new byte[numDurationBytes];

        for (int i = 0; i < track.notes.Count; i++)
        {
            int durationIndex = durationTable[track.notes[i].durationRaw];
            compressedTrack.rawDurationIndices[i] = durationIndex;

            int writeBitIndex = i * compressedTrack.header.bitsPerDuration;
            CompressedWrite(compressedTrack.compressedDurations, writeBitIndex, compressedTrack.header.bitsPerDuration, durationIndex);
        }

        return compressedTrack;
    }

    private static void CompressedWrite(byte[] buffer, int bitIndex, int numBits, int value)
    {
        int remainingBits = numBits;
        int bitWriteHead = bitIndex;
        
        while (remainingBits > 0)
        {
            int byteToWrite = bitWriteHead / 8;
            int startBit = bitWriteHead % 8;
            int bitsToWrite = Math.Min(8 - startBit, remainingBits);

            byte writeMask = (byte)(Math.Pow(2, bitsToWrite) - 1);
            byte writeVal = (byte)(((value & writeMask) << startBit) & 0xFF);

            buffer[byteToWrite] |= writeVal;

            remainingBits -= bitsToWrite;
            bitWriteHead += bitsToWrite;
            value >>= bitsToWrite;
        }
    }

    public void GenerateHeaderCode(string path)
    {
        Console.WriteLine($"\nWriting out codegen to '{path}'");

        string semitoneTableSerialized = 
            string.Join(", ",
                semitoneFrequencyTable
                .Select(x => x.ToString())
            );

        string durationTableSerialized = 
            string.Join(", ",
                durationMsTable
                .Select(x => x.ToString())
            );

        string compressedSemitonesSerialized = 
            string.Join(", ",
                compressedSemitones
                .Select(x => $"0x{x:X2}")
            );

        string compressedDurationsSerialized = 
            string.Join(", ",
                compressedDurations
                .Select(x => $"0x{x:X2}")
            );

        Directory.CreateDirectory(Path.GetDirectoryName(path));
        using StreamWriter headerFile = new StreamWriter(path);

        headerFile.Write(
@$"#pragma once

#include <Arduino.h>
#include <inttypes.h>

// ============================================================================
// ----------------------- Autogenerated by ArduinoMIDI -----------------------
// ============================================================================

// Compressed track header
#define NUM_NOTES {header.numNotes}
#define NUM_UNIQUE_SEMITONES {header.numUniqueSemitones}
#define NUM_UNIQUE_DURATIONS {header.numUniqueDurations}
#define BITS_PER_SEMITONE {header.bitsPerSemitone}
#define BITS_PER_DURATION {header.bitsPerDuration}

// Mapping tables
const PROGMEM uint32_t semitoneFrequencyTable[NUM_UNIQUE_SEMITONES] = {{{semitoneTableSerialized}}};
const PROGMEM uint32_t durationMsTable[NUM_UNIQUE_DURATIONS] = {{{durationTableSerialized}}};

// Raw compressed data
#define COMPRESSED_SEMITONES_SIZE {compressedSemitones.Length}
#define COMPRESSED_DURATIONS_SIZE {compressedDurations.Length}
const PROGMEM uint8_t compressedSemitones[COMPRESSED_SEMITONES_SIZE] = {{{compressedSemitonesSerialized}}};
const PROGMEM uint8_t compressedDurations[COMPRESSED_DURATIONS_SIZE] = {{{compressedDurationsSerialized}}};

// ============================================================================
// Uncompressed Track Data
// frequency (Hz), duration (ms)
// ============================================================================
"
        );

        // Write out uncompressed data as comments for debugging
        for (int i = 0; i < header.numNotes; i++)
        {
            int frequency = semitoneFrequencyTable[rawSemitoneIndices[i]];
            int duration = durationMsTable[rawDurationIndices[i]];
            headerFile.WriteLine($"// {frequency}\t {duration}");
        }

        headerFile.Close();
    }
}