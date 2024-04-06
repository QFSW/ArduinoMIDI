using Melanchall.DryWetMidi.Core;

public static class Program
{
    static void Main(string[] args)
    {
        Console.WriteLine("ArduinoMIDI booting...");

        if (args.Length == 0)
        {
            Console.WriteLine("Error: Please provide the .midi file to process as the first argument");
            return;
        }

        if (!args[0].EndsWith(".mid"))
        {
            Console.WriteLine("Error: provided file is not a .mid");
            return;
        }

        MidiFile midiFile = MidiFile.Read(args[0]);
        Console.WriteLine($"Loaded midi '{args[0]}'");
        
        Track track = Track.Build(midiFile);
        track.RemoveBlanks(30);
        track.SeperateRepeatNotes();

        CompressedTrack compressedTrack = CompressedTrack.Build(track);
        compressedTrack.GenerateHeaderCode("out/track_codegen.h");

        Console.WriteLine("\nArduinoMIDI complete!");
    }
}