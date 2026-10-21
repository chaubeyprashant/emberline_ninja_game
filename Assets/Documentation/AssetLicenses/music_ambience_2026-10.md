# Music and ambience beds — October 2026

Ten clips under `Assets/Resources/Art/Audio/Music/`, downloaded 2026-10-21 from
OpenGameArt. Every licence below was read on the asset's own page that day.

| File | Asset | Creator | Licence | Attribution required | Source |
| --- | --- | --- | --- | --- | --- |
| `explore_theme.ogg` | Shinrin-Yoku | Patrick de Arteaga (patrickdearteaga.com; page lists uploader Varon Kein) | CC BY 4.0 | Yes — "Patrick de Arteaga, patrickdearteaga.com" (the author's requested form) | https://opengameart.org/content/shinrin-yoku |
| `combat_theme.ogg` | Taiko drums (seamless loop) | jobro; cut by congusbongus | CC BY 3.0 | Yes | https://opengameart.org/content/taiko-drums-seamless-loop (original: https://freesound.org/people/jobro/sounds/112248/) |
| `boss_theme.ogg` | Samurai | TAD | CC BY 4.0 | Yes | https://opengameart.org/content/samurai |
| `forest_ambience.ogg` | Park ambiences — birds | Thimras | CC0 1.0 | No | https://opengameart.org/content/park-ambiences |
| `river_ambience.ogg` | Park ambiences — river | Thimras | CC0 1.0 | No | same |
| `mountain_wind.ogg` | Park ambiences — wind | Thimras | CC0 1.0 | No | same |
| `fire_ambience.ogg` | Wood-Burning on Fireplace Sound Effect loop | PagDev | CC0 1.0 | No | https://opengameart.org/content/fireplace-sound-loop |
| `rain_ambience.ogg` | Rain in the Gutter Loop | Ogrebane | CC0 1.0 | No | https://opengameart.org/content/rain-gutter-loop |
| `night_ambience.ogg` | Crickets Ambient Noise - loopable | Wolfgang_ | CC0 1.0 | No | https://opengameart.org/content/crickets-ambient-noise-loopable |
| `village_ambience.ogg` | AMB Morning Sounds (Perfect Loop) | Kresiek The Furry | CC0 1.0 | No | https://opengameart.org/content/amb-morning-sounds-perfect-loop |

**Processing (ffmpeg 9.0.1):** music re-levelled to -16 LUFS integrated, beds to -24 LUFS,
with one fixed gain each (no dynamic loudness, which would break the loop points) and a
-1 dBFS limiter; 44.1 kHz stereo Vorbis at 160 kb/s. The Park recordings are 74–99 MB WAVs:
only their first ~13–17 MB was fetched (HTTP range), cut to 56 s (wind 50 s) and
looped by crossfading the 4 s after the cut over the opening. The morning bed was looped
the same way (58 s) because its own loop point jumped 6 dB. Loop seams measured within
±2.5 dB RMS over 0.5 s either side.

**Attribution in game:** the three CC BY tracks join the CC BY weapons and the marsh loop
in `Assets/Art/CREDITS.txt`. The audit's open item stands: those credits must be surfaced
in an in-game credits screen before a store build that ships them.
