using Melanchall.DryWetMidi.Core;
using Melanchall.DryWetMidi.Interaction;

using MidiNote = Melanchall.DryWetMidi.Interaction.Note;

public struct RawNote
{
    public int semiTone; // -1 = OFF
    public long durationRaw;
}

public class Track
{
    public List<RawNote> notes = new List<RawNote>();

    public static Track Build(MidiFile midiFile)
    {
        Console.WriteLine("\nBuilding raw track");

        Track track = new Track();
        long lastNoteEnd = 0;

        foreach (MidiNote midiNote in midiFile.GetNotes())
        {
            // Can't support overlapping notes
            if (lastNoteEnd > midiNote.Time)
            {
                Console.WriteLine("Overlapping note - this will be cut off early");
            }

            // Insert a blank if needed
            if (lastNoteEnd < midiNote.Time)
            {
                RawNote blank;
                blank.semiTone = -1;
                blank.durationRaw = midiNote.Time - lastNoteEnd;
                track.notes.Add(blank);
            }

            RawNote note;
            note.semiTone = midiNote.NoteNumber;
            note.durationRaw = midiNote.Length;
            track.notes.Add(note);

            lastNoteEnd = midiNote.Time + midiNote.Length;
        }

        return track;
    }

    public void RemoveBlanks(long maxDurationToStrip)
    {
        for (int i = notes.Count - 1; i >= 0; i--)
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

    public void DumpToConsole()
    {
        foreach (RawNote note in notes)
        {
            Console.WriteLine($"{note.semiTone} - {note.durationRaw}");
        }
    }
}