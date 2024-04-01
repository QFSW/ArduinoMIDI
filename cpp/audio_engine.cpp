#include "audio_engine.h"

#include <Arduino.h>

// Include the codegen from ArduinoMIDI
#include "track_codegen.h"

void AudioEngine::start()
{
    _curr_note_index = 0;
    _curr_note_remaining = decompress_duration(0);

    const int frequency = decompress_frequency(0);
    tone(AUDIO_PIN, frequency);
}

void AudioEngine::tick()
{
    int elapsedTime = millis() - _last_tick;
    _curr_note_remaining -= elapsedTime;

    // Keep advacing to next note if current one is done
    if (_curr_note_remaining <= 0)
    {
        _curr_note_index++;
        if (_curr_note_index >= NUM_NOTES)
        {
            _curr_note_index = 0;
        }

        _curr_note_remaining += decompress_duration(_curr_note_index);
        const int frequency = decompress_frequency(_curr_note_index);
        tone(AUDIO_PIN, frequency);
    }

    _last_tick = millis();
}

int AudioEngine::decompress_frequency(int note_index) const
{
    const uint32_t bitIndex = note_index * BITS_PER_SEMITONE;
    const uint32_t semitoneIndex = decompress(compressedSemitones, bitIndex, BITS_PER_SEMITONE);

    return pgm_read_word(semitoneFrequencyTable + semitoneIndex);
}

int AudioEngine::decompress_duration(int note_index) const
{
    const uint32_t bitIndex = note_index * BITS_PER_DURATION;
    const uint32_t durationIndex = decompress(compressedDurations, bitIndex, BITS_PER_DURATION);

    return pgm_read_word(durationMsTable + durationIndex);
}

uint32_t AudioEngine::decompress(const uint8_t* buffer, uint32_t bitIndex, uint8_t numBits) const
{
    uint32_t result = 0;
    uint8_t writeBit = 0;

    uint8_t remainingBits = numBits;
    uint32_t bitReadHead = bitIndex;

    while (remainingBits > 0)
    {
        uint32_t const byteToRead = bitReadHead >> 3; //  x / 8
        uint8_t const readBit = bitReadHead & 0b111; // x % 8;
        uint8_t const bitsToRead = min(8 - readBit, remainingBits);

        uint32_t const readMask = (1 << bitsToRead) - 1; // 2 ^ x - 1;
        uint32_t const fullByte = pgm_read_byte(buffer + byteToRead);
        uint32_t const extractedVal = ((fullByte >> readBit) & readMask);

        result |= (extractedVal << writeBit);

        writeBit += bitsToRead;
        remainingBits -= bitsToRead;
        bitReadHead += bitsToRead;
    }
    
    return result;
}