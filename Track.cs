using Melanchall.DryWetMidi.Core;
using Melanchall.DryWetMidi.Interaction;

using MidiNote = Melanchall.DryWetMidi.Interaction.Note;

public struct RawNote
{
    public int semiTone; // -1 = OFF
    public long durationRaw;
}

public struct Chord
{
    public List<int> semiTones;
    public long startTime;
    public long durationRaw;

    public long EndTime => startTime + durationRaw;

    public RawNote Reduce()
    {
        // For now always use the highest note in the chord
        RawNote note;
        note.semiTone = semiTones.Max();
        note.durationRaw = durationRaw;

        return note;
    }
}

public class Track
{
    public TempoMap tempoMap;
    public List<RawNote> notes = new List<RawNote>();

    public static Track Build(MidiFile midiFile)
    {
        Console.WriteLine("\nBuilding raw track");

        Track track = new Track();
        track.tempoMap = midiFile.GetTempoMap();

        Chord currChord;
        currChord.semiTones = new List<int>();
        currChord.startTime = 0;
        currChord.durationRaw = 0;

        foreach (MidiNote midiNote in midiFile.GetNotes())
        {
            if (currChord.startTime == midiNote.Time && currChord.durationRaw == midiNote.Length)
            {
                // New note is part of chord, add it in
                currChord.semiTones.Add(midiNote.NoteNumber);
            }
            else
            {
                // New chord, terminate the old one
                // If we have overlapping chords, cut the existing one short
                if (currChord.EndTime > midiNote.Time)
                {
                    currChord.durationRaw = midiNote.Time - currChord.startTime;
                }

                // Reduce the last chord to a note
                if (currChord.semiTones.Any())
                {
                    track.notes.Add(currChord.Reduce());
                }

                // Insert a blank if needed
                if (currChord.EndTime < midiNote.Time)
                {
                    RawNote blank;
                    blank.semiTone = -1;
                    blank.durationRaw = midiNote.Time - currChord.EndTime;
                    track.notes.Add(blank);
                }

                // Start a new chord
                currChord.startTime = midiNote.Time;
                currChord.durationRaw = midiNote.Length;
                currChord.semiTones.Clear();
                currChord.semiTones.Add(midiNote.NoteNumber);
            }
        }

        // Reduce final chord
        if (currChord.semiTones.Any())
        {
            track.notes.Add(currChord.Reduce());
        }

        return track;
    }

    public void RemoveBlanks(long maxDurationToStrip)
    {
        for (int i = notes.Count - 1; i >= 1; i--)
        {
            if (notes[i].semiTone == -1 && notes[i].durationRaw < maxDurationToStrip)
            {
                RawNote mergeNote = notes[i - 1];
                mergeNote.durationRaw += notes[i].durationRaw;
                notes[i - 1] = mergeNote;

                notes.RemoveAt(i);
            }
        }
    }

    public void SeperateRepeatNotes()
    {
        MetricTimeSpan duration1ms = new MetricTimeSpan(0, 0, 0, 2);
        long blankDuration = Math.Max(1, TimeConverter.ConvertFrom(duration1ms, tempoMap));

        for (int i = 0; i < notes.Count - 1; i++)
        {
            if (notes[i].semiTone == notes[i + 1].semiTone)
            {
                RawNote shortenedNote = notes[i];
                shortenedNote.durationRaw -= blankDuration;
                notes[i] = shortenedNote;

                RawNote blank;
                blank.semiTone = -1;
                blank.durationRaw = blankDuration;
                notes.Insert(i + 1, blank);
            }
        }
    }

    public void DumpToConsole()
    {
        foreach (RawNote note in notes)
        {
            Console.WriteLine($"{note.semiTone} - {note.durationRaw}");
        }
    }
}