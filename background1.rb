# Street Fighter-ish mobile game BGM loop (Sonic Pi)
use_bpm 150

# --- Scale / tonal center ---
root = :e2
scale_notes = scale(:e3, :minor_pentatonic, num_octaves: 2)

# --- Master FX bus vibe ---
with_fx :reverb, room: 0.5, mix: 0.25 do
  with_fx :compressor, threshold: 0.2, slope_above: 0.5 do
    
    # DRUMS: punchy + syncopated
    live_loop :drums do
      # 2-bar pattern (8 beats)
      16.times do |i|
        # Kick
        if [0, 4, 8, 11, 12].include?(i)
          sample :bd_tek, amp: 1.6
        end
        
        # Snare / clap on backbeats with a little variation
        if [4, 12].include?(i)
          sample :sn_dolf, amp: 1.2
        end
        
        # Closed hat constant 16ths, with accents
        hat_amp = (i % 4 == 2) ? 0.55 : 0.4
        sample :drum_cymbal_closed, amp: hat_amp, sustain: 0, release: 0.05
        
        # Occasional open hat for lift
        if [7, 15].include?(i)
          sample :drum_cymbal_open, amp: 0.35, sustain: 0, release: 0.25
        end
        
        sleep 0.25
      end
    end
    
    # BASS: bouncy, fighting-game energy
    live_loop :bass do
      sync :drums
      use_synth :fm
      with_fx :lpf, cutoff: 90 do
        pattern = (ring
                   :e2, :e2, :g2, :e2,  :a1, :a1, :b1, :e2,
                   :e2, :e2, :g2, :b1,  :a1, :b1, :e2, :r
                   )
        16.times do
          n = pattern.tick
          if n != :r
            play n, release: 0.15, amp: 1.0, depth: 1.5, divisor: 2
          end
          sleep 0.5
        end
      end
    end
    
    # CHORD STABS: arcade-style hits
    live_loop :stabs do
      sync :drums
      use_synth :saw
      with_fx :distortion, distort: 0.2 do
        with_fx :lpf, cutoff: 85 do
          prog = (ring
                  chord(:e3, :m7),
                  chord(:g3, :sus2),
                  chord(:a3, :m7),
                  chord(:b3, :sus4)
                  )
          4.times do
            c = prog.tick
            # syncopated hits over 2 beats
            play c, sustain: 0.1, release: 0.15, amp: 0.65
            sleep 0.75
            play c.choose, sustain: 0.05, release: 0.12, amp: 0.45
            sleep 1.25
          end
        end
      end
    end
    
    # LEAD: pentatonic riffs, changes every 2 bars
    live_loop :lead do
      sync :drums
      use_synth :pulse
      with_fx :echo, phase: 0.375, mix: 0.25, decay: 2.5 do
        with_fx :lpf, cutoff: 105 do
          2.times do
            16.times do
              n = scale_notes.choose
              # occasional rests for space
              if one_in(6)
                sleep 0.25
              else
                play n, release: 0.08, amp: 0.45, pulse_width: 0.2
                sleep 0.25
              end
            end
          end
          # little “turnaround” lick
          play_pattern_timed (ring :e4, :g4, :a4, :b4, :a4, :g4), (ring 0.125), release: 0.07, amp: 0.4
        end
      end
    end
    
    # PAD: subtle glue (keep low so it doesn't annoy in-game)
    live_loop :pad do
      sync :drums
      use_synth :hollow
      with_fx :lpf, cutoff: 75 do
        with_fx :slicer, phase: 0.5, mix: 0.15 do
          play chord(:e3, :m9), sustain: 4, release: 1.5, amp: 0.25
          sleep 4
          play chord(:a2, :m9), sustain: 4, release: 1.5, amp: 0.25
          sleep 4
        end
      end
    end
    
  end
end
