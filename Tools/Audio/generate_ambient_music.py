"""Synthesizes a seamless, relaxing ambient loop for the cyber school scene.

Output: Assets/Audio/Music/CyberSchoolAmbient.wav (44.1 kHz, 16-bit stereo).
Run from the project root:  python Tools/Audio/generate_ambient_music.py
"""

import os
import wave

import numpy as np

SAMPLE_RATE = 44100
CHORD_SECONDS = 8.0
SEED = 2026
OUTPUT_PATH = os.path.join("Assets", "Audio", "Music", "CyberSchoolAmbient.wav")
PEAK_DBFS = -4.0

# Dreamy D-major / lydian progression, two passes with a small variation.
# MIDI note numbers.
PROGRESSION = [
    [50, 57, 62, 66, 69, 76],  # Dmaj9
    [47, 54, 59, 62, 66, 73],  # Bm9
    [43, 50, 55, 59, 62, 69],  # Gmaj9 (add 9 on top)
    [45, 52, 57, 61, 64, 71],  # A add9
    [50, 57, 62, 66, 69, 76],  # Dmaj9
    [52, 59, 62, 66, 71, 74],  # Em9
    [43, 50, 55, 59, 62, 66],  # Gmaj7
    [45, 52, 57, 59, 64, 69],  # Asus2
]
BELL_SCALE = [74, 76, 78, 81, 83, 86, 88, 90]  # D major pentatonic, upper register


def midi_to_hz(note):
    return 440.0 * 2.0 ** ((note - 69) / 12.0)


def raised_cosine_window(length, fade):
    env = np.ones(length)
    ramp = 0.5 - 0.5 * np.cos(np.linspace(0.0, np.pi, fade))
    env[:fade] = ramp
    env[-fade:] = ramp[::-1]
    return env


def one_pole_lowpass(signal, cutoff_hz):
    # Applied in the frequency domain so it stays fast on long buffers.
    spectrum = np.fft.rfft(signal, axis=0)
    freqs = np.fft.rfftfreq(signal.shape[0], 1.0 / SAMPLE_RATE)
    response = 1.0 / np.sqrt(1.0 + (freqs / cutoff_hz) ** 2)
    return np.fft.irfft(spectrum * response[:, None], n=signal.shape[0], axis=0)


def render_pad(buffer, start, length, notes, rng):
    t = np.arange(length) / SAMPLE_RATE
    fade = int(3.0 * SAMPLE_RATE)
    env = raised_cosine_window(length, fade)
    for index, note in enumerate(notes):
        base = midi_to_hz(note)
        level = 0.16 if index == 0 else 0.11 / (1.0 + 0.15 * index)
        for channel, detune in enumerate((-0.0025, 0.0025)):
            freq = base * (1.0 + detune + rng.uniform(-0.0008, 0.0008))
            phase = rng.uniform(0, 2 * np.pi)
            lfo = 1.0 + 0.25 * np.sin(2 * np.pi * rng.uniform(0.05, 0.12) * t + phase)
            tone = np.sin(2 * np.pi * freq * t + phase)
            tone += 0.25 * np.sin(2 * np.pi * freq * 2.0 * t + phase * 1.3)
            tone += 0.08 * np.sin(2 * np.pi * freq * 3.0 * t + phase * 0.7)
            buffer[start:start + length, channel] += tone * lfo * env * level


def render_bass(buffer, start, length, root_note):
    t = np.arange(length) / SAMPLE_RATE
    env = raised_cosine_window(length, int(2.5 * SAMPLE_RATE))
    freq = midi_to_hz(root_note - 12)
    tone = np.sin(2 * np.pi * freq * t) * 0.18 * env
    buffer[start:start + length, :] += tone[:, None]


def render_bell(buffer, start, note, pan, level):
    length = int(4.0 * SAMPLE_RATE)
    t = np.arange(length) / SAMPLE_RATE
    freq = midi_to_hz(note)
    attack = np.minimum(t / 0.012, 1.0)
    env = attack * np.exp(-t * 1.4)
    modulator = np.sin(2 * np.pi * freq * 2.0 * t) * 0.6 * np.exp(-t * 3.0)
    tone = np.sin(2 * np.pi * freq * t + modulator) * env * level
    end = min(start + length, buffer.shape[0])
    tone = tone[: end - start]
    buffer[start:end, 0] += tone * (1.0 - pan)
    buffer[start:end, 1] += tone * pan


def make_reverb_ir(seconds, rng):
    length = int(seconds * SAMPLE_RATE)
    t = np.arange(length) / SAMPLE_RATE
    ir = np.zeros((length, 2))
    for channel in range(2):
        noise = rng.standard_normal(length)
        ir[:, channel] = noise * np.exp(-t * 2.2)
    ir[: int(0.02 * SAMPLE_RATE)] *= np.linspace(0.0, 1.0, int(0.02 * SAMPLE_RATE))[:, None]
    ir = one_pole_lowpass(ir, 3500.0)
    return ir / np.sqrt(np.sum(ir ** 2, axis=0, keepdims=True))


def circular_convolve(signal, ir):
    n = signal.shape[0]
    padded = np.zeros_like(signal)
    padded[: ir.shape[0]] = ir
    return np.fft.irfft(np.fft.rfft(signal, axis=0) * np.fft.rfft(padded, axis=0), n=n, axis=0)


def main():
    rng = np.random.default_rng(SEED)
    chord_len = int(CHORD_SECONDS * SAMPLE_RATE)
    loop_len = chord_len * len(PROGRESSION)
    overlap = int(3.0 * SAMPLE_RATE)
    tail = int(5.0 * SAMPLE_RATE)
    dry = np.zeros((loop_len + overlap + tail, 2))

    for index, chord in enumerate(PROGRESSION):
        start = index * chord_len
        render_pad(dry, start, chord_len + overlap, chord, rng)
        render_bass(dry, start, chord_len + overlap, chord[0])

        bell_time = rng.uniform(0.8, 2.0)
        while bell_time < CHORD_SECONDS - 0.5:
            note = int(rng.choice(BELL_SCALE))
            render_bell(dry, start + int(bell_time * SAMPLE_RATE), note,
                        pan=rng.uniform(0.25, 0.75), level=rng.uniform(0.05, 0.09))
            bell_time += rng.uniform(1.8, 3.6)

    # Fold the overhang onto the start so the loop point is seamless.
    dry[: dry.shape[0] - loop_len] += dry[loop_len:]
    dry = dry[:loop_len]

    dry = one_pole_lowpass(dry, 4200.0)
    wet = circular_convolve(dry, make_reverb_ir(4.5, rng))
    mix = dry * 0.55 + wet * 0.45 * (np.max(np.abs(dry)) / max(np.max(np.abs(wet)), 1e-9))

    mix -= np.mean(mix, axis=0)
    mix *= 10 ** (PEAK_DBFS / 20.0) / np.max(np.abs(mix))
    pcm = np.clip(mix * 32767.0, -32768, 32767).astype("<i2")

    os.makedirs(os.path.dirname(OUTPUT_PATH), exist_ok=True)
    with wave.open(OUTPUT_PATH, "wb") as wav:
        wav.setnchannels(2)
        wav.setsampwidth(2)
        wav.setframerate(SAMPLE_RATE)
        wav.writeframes(pcm.tobytes())

    seam = np.max(np.abs(pcm[0].astype(int) - pcm[-1].astype(int)))
    print(f"Wrote {OUTPUT_PATH}: {loop_len / SAMPLE_RATE:.1f}s, loop seam jump {seam}")


if __name__ == "__main__":
    main()
