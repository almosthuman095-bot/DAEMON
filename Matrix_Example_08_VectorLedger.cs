using System;
using System.Collections.Generic;
using MysticEyeStudios.Matrix.FieldPopulators;
using MysticEyeStudios.Matrix.Fields;
using UnityEngine;

/// <summary>
/// Matrix Example 10 — Audio File + Boredom Drive + Growth Hormone Octave Field.
///
/// The organism never resets and there are no discrete "problems".
/// Origin (0,0,0) is the persistent observer / pacemaker and fires every tick.
/// The world continuously presents a changing tone. A logarithmic auditory row
/// projects that tone into a stable spatial region of the persistent field.
/// A second row represents possible output frequencies.
///
/// The environmental rule is deliberately hidden from the field:
///     desired output frequency = input frequency * 2 (one octave up)
///
/// Correct output activity receives dopamine; wrong output activity receives pain.
/// Chemistry walks backward only through the temporary vector ledger written by
/// actual electrical transmission. Persistent connection vectors are never reset.
///
/// Important experiment rule:
///     activity does not fill the volume by itself.
///     Structural growth is local. Active input and all output interfaces emit
///     growth-hormone spheres; frontier structure follows the resulting gradient.
/// </summary>
[RequireComponent(typeof(AudioSource))]
public class Matrix_Example_10_AudioFileDrive : MonoBehaviour
{
    [Header("Clock")]
    [SerializeField, Range(0.05f, 10f)] private float secondsPerTick = 1f;
    [SerializeField] private bool paused;

    [Header("Audio File World")]
    [Tooltip("Drop an imported WAV/MP3/OGG AudioClip here. The clip itself drives the sensory field.")]
    [SerializeField] private AudioClip audioClip;
    [SerializeField] private bool loopAudio = true;
    [SerializeField] private bool playOnStart = true;
    [Tooltip("FFT size used to read the currently playing audio.")]
    [SerializeField, Range(256, 4096)] private int fftSize = 1024;
    [Tooltip("Normalized spectral energy required for a frequency channel to count as active.")]
    [SerializeField, Range(0.001f, 1f)] private float audioActivationThreshold = 0.08f;
    [Tooltip("Number of neighboring FFT bins averaged around each logarithmic frequency channel.")]
    [SerializeField, Range(0, 8)] private int fftNeighborhood = 2;

    [Tooltip("Lowest auditory channel in Hz.")]
    [SerializeField, Range(20f, 1000f)] private float minimumFrequencyHz = 125f;

    [Tooltip("Number of logarithmically spaced sensory/output frequency bins.")]
    [SerializeField, Range(7, 15)] private int frequencyBinCount = 11;

    [Tooltip("How many spatial bins equal one octave. Four gives quarter-octave spacing.")]
    [SerializeField, Range(1, 6)] private int binsPerOctave = 4;

    [SerializeField, Range(0f, 2f)] private float sensoryStrength = 0.55f;
    [SerializeField, Range(0, 2)] private int sensoryRadius = 0;
    [SerializeField, Range(0.01f, 1f)] private float correctDopamine = 0.75f;
    [SerializeField, Range(0f, 1f)] private float wrongOutputPain = 0.25f;
    [SerializeField, Range(0, 20)] private int consequenceDelayTicks = 1;
    [SerializeField, Range(0, 20)] private int outputCooldownTicks = 1;

    [Header("Persistent Brain Field")]
    [SerializeField, Range(6, 14)] private int brainRadius = 7;
    [SerializeField, Range(0.01f, 2f)] private float firingThreshold = 0.22f;
    [SerializeField, Range(0f, 1f)] private float potentialDecay = 0.55f;
    [SerializeField, Range(0f, 1f)] private float activationDecay = 0.15f;
    [SerializeField, Range(0f, 2f)] private float firingCost = 0.65f;
    [SerializeField, Range(0f, 0.25f)] private float spontaneousNoise = 0.0f;

    [Header("Persistent Vectors")]
    [SerializeField, Range(1, 12)] private int initialOriginVectors = 3;
    [SerializeField, Range(1, 16)] private int maximumVectorsPerCell = 6;
    [SerializeField, Range(1f, 5f)] private float growthRadius = 2.25f;
    [SerializeField, Range(0f, 1f)] private float growthChance = 0.22f;
    [SerializeField, Range(0.01f, 1f)] private float newVectorWeight = 0.10f;
    [SerializeField, Range(0.01f, 2f)] private float exploratoryPulse = 0.32f;
    [SerializeField, Range(1, 100)] private int topologyCheckTicks = 4;
    [SerializeField, Range(0f, 0.25f)] private float pruneWeight = 0.025f;

    [Header("Growth Hormone")]
    [Tooltip("Radius of the growth-hormone sphere around the currently active input and every output interface.")]
    [SerializeField, Range(1f, 10f)] private float interfaceGrowthRadius = 4.5f;
    [Tooltip("Hormone emitted by the currently active auditory input.")]
    [SerializeField, Range(0f, 2f)] private float activeInputGrowthStrength = 1.0f;
    [Tooltip("Weak persistent hormone emitted by every possible output interface.")]
    [SerializeField, Range(0f, 2f)] private float outputGrowthStrength = 0.55f;
    [Tooltip("Chance for active/frontier structure to explore when no hormone is nearby.")]
    [SerializeField, Range(0f, 1f)] private float backgroundGrowthChance = 0.08f;
    [Tooltip("Maximum growth chance while immersed in strong hormone.")]
    [SerializeField, Range(0f, 1f)] private float hormoneGrowthChance = 0.55f;
    [Tooltip("How strongly a hormone gradient steers a new branch. 0 = random, 1 = follow gradient.")]
    [SerializeField, Range(0f, 1f)] private float growthGradientInfluence = 0.85f;
    [Tooltip("Hormone concentration consumed locally when a branch is created.")]
    [SerializeField, Range(0f, 1f)] private float growthConsumption = 0.15f;

    [Header("Boredom / Drive")]
    [Tooltip("How much unresolved boredom accumulates every tick.")]
    [SerializeField, Range(0f, 0.25f)] private float boredomPerTick = 0.04f;
    [Tooltip("Maximum internal boredom/drive.")]
    [SerializeField, Range(0.1f, 10f)] private float maximumBoredom = 2.0f;
    [Tooltip("Boredom removed by making any output attempt, even a painful one.")]
    [SerializeField, Range(0f, 2f)] private float attemptBoredomRelief = 0.12f;
    [Tooltip("Additional boredom removed by dopamine/success.")]
    [SerializeField, Range(0f, 5f)] private float dopamineBoredomRelief = 1.0f;
    [Tooltip("How strongly boredom raises background structural exploration.")]
    [SerializeField, Range(0f, 2f)] private float boredomGrowthPressure = 0.65f;
    [Tooltip("How strongly boredom increases exploratory electrical pulses.")]
    [SerializeField, Range(0f, 2f)] private float boredomPulsePressure = 0.60f;

    [Header("Activity Ledger")]
    [SerializeField, Range(0f, 1f)] private float ledgerDecay = 0.86f;
    [SerializeField, Range(0.001f, 1f)] private float ledgerDeposit = 0.40f;
    [SerializeField, Range(0.001f, 1f)] private float minimumLedger = 0.02f;

    [Header("Chemistry")]
    [SerializeField, Range(0f, 1f)] private float chemicalDecay = 0.97f;
    [SerializeField, Range(0.001f, 1f)] private float minimumChemical = 0.015f;
    [SerializeField, Range(0.001f, 1f)] private float vectorPlasticity = 0.18f;
    [SerializeField, Range(0f, 0.25f)] private float minimumPlasticity = 0.03f;

    [Header("Visualization")]
    [SerializeField, Range(0.10f, 0.75f)] private float cellRenderGridSize = 0.25f;
    [SerializeField, Range(0.10f, 0.75f)] private float chemicalRenderGridSize = 0.35f;
    [SerializeField, Range(0.10f, 0.75f)] private float cellRadius = 0.28f;
    [SerializeField, Range(0.05f, 0.60f)] private float chemicalRadius = 0.20f;
    [SerializeField] private bool renderCells = true;
    [SerializeField] private bool renderChemistry = true;
    [SerializeField] private bool renderGrowthHormone = true;
    [SerializeField, Range(0.10f, 0.75f)] private float growthRenderGridSize = 0.35f;
    [SerializeField, Range(0.01f, 1f)] private float growthRenderThreshold = 0.08f;
    [SerializeField] private bool drawVectorGizmos = true;
    [SerializeField] private bool drawFrequencyLabels = true;

    private readonly Vector3Int origin = Vector3Int.zero;

    private MutableFieldAudio10<BrainCellAudio10> brainField;
    private MutableFieldAudio10<ChemicalCellAudio10> chemicalField;
    private MutableFieldAudio10<LedgerCellAudio10> ledgerField;
    private MutableFieldAudio10<FrequencyPortAudio10> sensoryField;
    private MutableFieldAudio10<FrequencyPortAudio10> outputField;

    private readonly List<Vector3Int> sensoryAddresses = new();
    private readonly List<Vector3Int> outputAddresses = new();
    private readonly List<PulseAudio10> exploratoryPulses = new();
    private readonly List<PendingConsequenceAudio10> pendingConsequences = new();
    private readonly Dictionary<ulong, float[]> chainSpectra = new();
    private readonly Dictionary<Vector3Int, float> consumedGrowthHormone = new();

    private IFieldPopulator<Vector3, Color?> cellRenderField;
    private IFieldPopulator<Vector3, Color?> chemistryRenderField;
    private IFieldPopulator<Vector3, Color?> growthRenderField;
    private MarchingCubesInterpreter<Color?> cellInterpreter;
    private MarchingCubesInterpreter<Color?> chemistryInterpreter;
    private MarchingCubesInterpreter<Color?> growthInterpreter;

    private System.Random random;
    private float clock;
    private long tick;
    private ulong nextChainId = 1;
    private ulong currentChainId;
    private int outputCooldown;
    private float boredom;
    private AudioSource audioSource;
    private float[] fftBuffer;
    private float[] currentSpectrumBins;

    private int currentInputBin;
    private int expectedOutputBin;
    private float currentInputHz;
    private float expectedOutputHz;
    private int lastOutputBin = -1;
    private string lastEvent = "Born.";

    private long outputAttempts;
    private long correctOutputs;
    private readonly Queue<bool> recentResults = new();
    private int recentCorrect;
    private const int RecentWindow = 100;

    private void Start()
    {
        random = new System.Random(1337);
        audioSource = GetComponent<AudioSource>();
        audioSource.clip = audioClip;
        audioSource.loop = loopAudio;
        audioSource.playOnAwake = false;
        fftSize = ClosestPowerOfTwo(Mathf.Clamp(fftSize, 256, 4096));
        fftBuffer = new float[fftSize];
        currentSpectrumBins = new float[frequencyBinCount];
        if (playOnStart && audioClip != null) audioSource.Play();

        brainField = new MutableFieldAudio10<BrainCellAudio10>();
        chemicalField = new MutableFieldAudio10<ChemicalCellAudio10>();
        ledgerField = new MutableFieldAudio10<LedgerCellAudio10>();
        sensoryField = new MutableFieldAudio10<FrequencyPortAudio10>();
        outputField = new MutableFieldAudio10<FrequencyPortAudio10>();

        ValidateLayout();
        BuildFrequencyRows();
        Birth();
        SampleAudioSpectrum();

        cellRenderField = new FormulaPopulator<Vector3, Color?>(ProbeCellGeometry);
        chemistryRenderField = new FormulaPopulator<Vector3, Color?>(ProbeChemistryGeometry);
        growthRenderField = new FormulaPopulator<Vector3, Color?>(ProbeGrowthHormoneGeometry);
        cellInterpreter = new MarchingCubesInterpreter<Color?>(cellRenderField);
        chemistryInterpreter = new MarchingCubesInterpreter<Color?>(chemistryRenderField);
        growthInterpreter = new MarchingCubesInterpreter<Color?>(growthRenderField);

        Render();
    }

    private void OnDestroy()
    {
        cellInterpreter?.Dispose();
        chemistryInterpreter?.Dispose();
        growthInterpreter?.Dispose();
    }

    private void Update()
    {
        if (paused)
            return;

        clock += Time.deltaTime;
        if (clock < secondsPerTick)
            return;

        clock = 0f;
        LifeTick();
    }

    private void OnValidate()
    {
        ValidateLayout();
    }

    private void ValidateLayout()
    {
        frequencyBinCount = Mathf.Clamp(frequencyBinCount, 7, 15);
        binsPerOctave = Mathf.Clamp(binsPerOctave, 1, frequencyBinCount - 2);

        // Need enough X space to lay the frequency row across the field.
        int requiredHalfWidth = Mathf.CeilToInt((frequencyBinCount - 1) * 0.5f);
        brainRadius = Mathf.Max(brainRadius, requiredHalfWidth + 1, 6);
    }

    private void BuildFrequencyRows()
    {
        sensoryAddresses.Clear();
        outputAddresses.Clear();

        int half = (frequencyBinCount - 1) / 2;
        int sensoryZ = -brainRadius;
        int outputZ = brainRadius;

        for (int i = 0; i < frequencyBinCount; i++)
        {
            int x = i - half;
            Vector3Int sensoryAddress = new Vector3Int(x, 0, sensoryZ);
            Vector3Int outputAddress = new Vector3Int(x, 0, outputZ);
            float hz = FrequencyForBin(i);

            sensoryAddresses.Add(sensoryAddress);
            outputAddresses.Add(outputAddress);

            sensoryField.Set(sensoryAddress, new FrequencyPortAudio10
            {
                Bin = i,
                FrequencyHz = hz,
                Value = 0f
            });

            outputField.Set(outputAddress, new FrequencyPortAudio10
            {
                Bin = i,
                FrequencyHz = hz,
                Value = 0f
            });
        }
    }

    private float FrequencyForBin(int bin)
    {
        return minimumFrequencyHz * Mathf.Pow(2f, bin / (float)binsPerOctave);
    }

    private static int ClosestPowerOfTwo(int value)
    {
        int p = 256;
        while (p < value && p < 4096) p <<= 1;
        return p;
    }

    private void SampleAudioSpectrum()
    {
        if (currentSpectrumBins == null || currentSpectrumBins.Length != frequencyBinCount)
            currentSpectrumBins = new float[frequencyBinCount];

        Array.Clear(currentSpectrumBins, 0, currentSpectrumBins.Length);

        if (audioSource == null || audioSource.clip == null || !audioSource.isPlaying)
        {
            for (int i = 0; i < sensoryAddresses.Count; i++)
            {
                FrequencyPortAudio10 port = sensoryField.Probe(sensoryAddresses[i]);
                if (port == null) continue;
                port.Value = 0f;
                sensoryField.Set(sensoryAddresses[i], port);
            }
            currentInputBin = -1;
            currentInputHz = 0f;
            return;
        }

        if (fftBuffer == null || fftBuffer.Length != fftSize)
            fftBuffer = new float[fftSize];

        audioSource.GetSpectrumData(fftBuffer, 0, FFTWindow.BlackmanHarris);
        float nyquist = AudioSettings.outputSampleRate * 0.5f;
        float hzPerFftBin = nyquist / fftBuffer.Length;
        float maximum = 0f;
        int strongest = -1;

        for (int i = 0; i < frequencyBinCount; i++)
        {
            float hz = FrequencyForBin(i);
            int center = Mathf.Clamp(Mathf.RoundToInt(hz / hzPerFftBin), 0, fftBuffer.Length - 1);
            int lo = Mathf.Max(0, center - fftNeighborhood);
            int hi = Mathf.Min(fftBuffer.Length - 1, center + fftNeighborhood);
            float energy = 0f;
            for (int b = lo; b <= hi; b++) energy += fftBuffer[b];
            energy /= Mathf.Max(1, hi - lo + 1);
            currentSpectrumBins[i] = energy;
            if (energy > maximum) { maximum = energy; strongest = i; }
        }

        // Normalize only inside our sensory representation. This preserves the shape
        // of the real audio spectrum without injecting synthetic/random tones.
        if (maximum > 0.0000001f)
            for (int i = 0; i < currentSpectrumBins.Length; i++)
                currentSpectrumBins[i] = Mathf.Clamp01(currentSpectrumBins[i] / maximum);

        currentInputBin = strongest;
        currentInputHz = strongest >= 0 ? FrequencyForBin(strongest) : 0f;
        expectedOutputBin = strongest >= 0 ? strongest + binsPerOctave : -1;
        expectedOutputHz = expectedOutputBin >= 0 && expectedOutputBin < frequencyBinCount
            ? FrequencyForBin(expectedOutputBin) : 0f;

        for (int i = 0; i < sensoryAddresses.Count; i++)
        {
            FrequencyPortAudio10 port = sensoryField.Probe(sensoryAddresses[i]);
            if (port == null) continue;
            float value = currentSpectrumBins[i];
            port.Value = value >= audioActivationThreshold ? value : 0f;
            sensoryField.Set(sensoryAddresses[i], port);
        }
    }

    private void Birth()
    {
        BrainCellAudio10 root = GetOrCreateCell(origin);

        for (int i = 0; i < initialOriginVectors; i++)
        {
            Vector3Int target = RandomNearby(origin, growthRadius);
            AddVector(root, target - origin, Range(0.08f, 0.20f));
        }

        brainField.Set(origin, root);
    }

    private void LifeTick()
    {
        tick++;
        currentChainId = nextChainId++;
        if (nextChainId == 0)
            nextChainId = 1;

        SampleAudioSpectrum();

        // Capture the real spectral pattern that existed when this electrical
        // chain began. Delayed outputs are judged against this historical audio
        // frame rather than whatever part of the MP3 is playing later.
        chainSpectra[currentChainId] = (float[])currentSpectrumBins.Clone();
        PruneOldChainContexts();

        // Persistent observer / pacemaker. This never moves and never resets.
        BrainCellAudio10 root = GetOrCreateCell(origin);
        root.ChainId = currentChainId;
        root.Potential += 1f;
        root.Activation = 1f;
        root.LastFiredTick = tick;
        brainField.Set(origin, root);

        ProjectCurrentSound();
        Propagate();
        UpdateOutputsAndConsequences();
        DecayLedger();
        StepChemistry();
        StepPendingConsequences();

        if (outputCooldown > 0)
            outputCooldown--;

        DecayConsumedGrowthHormone();

        // Doing nothing is not free. Boredom is an organism-level drive: it
        // accumulates while life continues, increases exploration pressure, and
        // is relieved by interaction. Pain can make a route unattractive, but it
        // cannot make permanent inactivity the safest equilibrium.
        boredom = Mathf.Min(maximumBoredom, boredom + boredomPerTick);

        if (topologyCheckTicks > 0 && tick % topologyCheckTicks == 0)
            StructuralPlasticity();

        Render();
    }


    private void PruneOldChainContexts()
    {
        // Chemistry/ledger are deliberately short-lived. Keep enough causal
        // world history for delayed propagation without allowing this lookup
        // to become long-term memory. The persistent brain remains the vectors.
        ulong keepWindow = 256;
        if (currentChainId <= keepWindow)
            return;

        ulong minimum = currentChainId - keepWindow;
        List<ulong> remove = null;

        foreach (var pair in chainSpectra)
        {
            if (pair.Key >= minimum)
                continue;

            if (remove == null)
                remove = new List<ulong>();
            remove.Add(pair.Key);
        }

        if (remove == null)
            return;

        for (int i = 0; i < remove.Count; i++)
            chainSpectra.Remove(remove[i]);
    }

    private void ProjectCurrentSound()
    {
        // A real audio frame can activate many frequency regions simultaneously.
        // Each active spectral band perturbs its own stable sensory location.
        for (int bin = 0; bin < sensoryAddresses.Count; bin++)
        {
            FrequencyPortAudio10 port = sensoryField.Probe(sensoryAddresses[bin]);
            if (port == null || port.Value <= 0f) continue;

            Vector3Int center = sensoryAddresses[bin];
            for (int x = -sensoryRadius; x <= sensoryRadius; x++)
                for (int y = -sensoryRadius; y <= sensoryRadius; y++)
                    for (int z = -sensoryRadius; z <= sensoryRadius; z++)
                    {
                        Vector3Int offset = new Vector3Int(x, y, z);
                        if (offset.sqrMagnitude > sensoryRadius * sensoryRadius) continue;
                        Vector3Int address = center + offset;
                        if (!InsideBrain(address)) continue;

                        BrainCellAudio10 cell = GetOrCreateCell(address);
                        float falloff = sensoryRadius <= 0 ? 1f : 1f - offset.magnitude / (sensoryRadius + 1f);
                        cell.Potential += sensoryStrength * port.Value * Mathf.Max(0.15f, falloff);
                        cell.ChainId = currentChainId;
                        brainField.Set(address, cell);
                    }
        }
    }

    private void Propagate()
    {
        Dictionary<Vector3Int, List<SignalAudio10>> incoming = new();
        Dictionary<Vector3Int, List<CauseAudio10>> causes = new();

        for (int i = 0; i < exploratoryPulses.Count; i++)
        {
            PulseAudio10 pulse = exploratoryPulses[i];
            AddIncoming(incoming, pulse.Target, pulse.Strength, pulse.ChainId);
            AddCause(causes, pulse.Target, pulse.Source, pulse.Strength, pulse.ChainId);
        }
        exploratoryPulses.Clear();

        List<KeyValuePair<Vector3Int, BrainCellAudio10>> snapshot =
            new List<KeyValuePair<Vector3Int, BrainCellAudio10>>(brainField.GetValues());

        for (int n = 0; n < snapshot.Count; n++)
        {
            Vector3Int sourceAddress = snapshot[n].Key;
            BrainCellAudio10 source = snapshot[n].Value;

            for (int i = 0; i < source.Vectors.Count; i++)
            {
                BrainVectorAudio10 vector = source.Vectors[i];
                Vector3Int target = sourceAddress + vector.Offset;
                float signal = 0f;

                if (source.Activation > 0.05f && InsideBrain(target))
                {
                    signal = source.Activation * vector.Weight;
                    AddIncoming(incoming, target, signal, source.ChainId);
                    AddCause(causes, target, sourceAddress, signal, source.ChainId);
                }

                vector.LastSignal = signal;
                vector.Age++;
                source.Vectors[i] = vector;
            }

            brainField.Set(sourceAddress, source);
        }

        HashSet<Vector3Int> addresses = new();
        foreach (var pair in brainField.GetValues()) addresses.Add(pair.Key);
        foreach (var pair in incoming) addresses.Add(pair.Key);

        foreach (Vector3Int address in addresses)
        {
            BrainCellAudio10 cell = GetOrCreateCell(address);
            float totalInput = 0f;
            ulong dominantChain = cell.ChainId;
            float dominantStrength = 0f;

            if (incoming.TryGetValue(address, out List<SignalAudio10> signals))
            {
                Dictionary<ulong, float> totals = new();
                for (int i = 0; i < signals.Count; i++)
                {
                    SignalAudio10 s = signals[i];
                    totalInput += s.Strength;
                    if (s.ChainId == 0) continue;
                    if (!totals.ContainsKey(s.ChainId)) totals[s.ChainId] = 0f;
                    totals[s.ChainId] += Mathf.Abs(s.Strength);
                }

                foreach (var pair in totals)
                {
                    if (pair.Value <= dominantStrength) continue;
                    dominantStrength = pair.Value;
                    dominantChain = pair.Key;
                }
            }

            if (address != origin)
                totalInput += Range(-spontaneousNoise, spontaneousNoise);

            cell.Potential = cell.Potential * potentialDecay + totalInput;
            bool fired = address == origin || cell.Potential >= firingThreshold;
            cell.Activation = fired ? 1f : cell.Activation * activationDecay;

            if (fired)
            {
                cell.LastFiredTick = tick;
                cell.Potential -= firingCost;
                if (address == origin) cell.ChainId = currentChainId;
                else if (dominantChain != 0) cell.ChainId = dominantChain;

                if (causes.TryGetValue(address, out List<CauseAudio10> causal))
                {
                    for (int i = 0; i < causal.Count; i++)
                    {
                        CauseAudio10 c = causal[i];
                        if (c.ChainId == 0 || c.ChainId != cell.ChainId || c.Signal <= 0.001f)
                            continue;

                        RecordFiring(c.Source, address, c.Signal, c.ChainId);
                    }
                }
            }

            brainField.Set(address, cell);
        }
    }

    private void UpdateOutputsAndConsequences()
    {
        for (int i = 0; i < outputAddresses.Count; i++)
        {
            Vector3Int address = outputAddresses[i];
            BrainCellAudio10 cell = brainField.Probe(address);
            FrequencyPortAudio10 output = outputField.Probe(address);
            if (output == null) continue;

            bool fired = cell != null && cell.LastFiredTick == tick;
            output.Value = cell?.Activation ?? 0f;
            outputField.Set(address, output);

            if (!fired || outputCooldown > 0)
                continue;

            // The brain is persistent and may take several ticks to move a
            // sensory event to an action. Judge this output against the input
            // carried by its causal ChainId, not against the newest random tone.
            float[] causalSpectrum;
            if (!chainSpectra.TryGetValue(cell.ChainId, out causalSpectrum))
                causalSpectrum = currentSpectrumBins;

            int sourceBin = i - binsPerOctave;
            float sourceEnergy = sourceBin >= 0 && sourceBin < causalSpectrum.Length
                ? causalSpectrum[sourceBin]
                : 0f;
            bool correct = sourceEnergy >= audioActivationThreshold;
            int causalInputBin = sourceBin;
            int causalExpectedBin = i;
            float causalInputHz = sourceBin >= 0 ? FrequencyForBin(sourceBin) : 0f;

            outputAttempts++;
            lastOutputBin = i;
            boredom = Mathf.Max(0f, boredom - attemptBoredomRelief);
            RecordResult(correct);

            pendingConsequences.Add(new PendingConsequenceAudio10
            {
                RemainingTicks = consequenceDelayTicks,
                OutputBin = i,
                InputBin = causalInputBin,
                ExpectedBin = causalExpectedBin,
                ChainId = cell.ChainId,
                Dopamine = correct ? correctDopamine : 0f,
                Pain = correct ? 0f : wrongOutputPain
            });

            outputCooldown = outputCooldownTicks;
            lastEvent = correct
                ? $"{causalInputHz:0} Hz -> {output.FrequencyHz:0} Hz : OCTAVE, dopamine pending"
                : $"{causalInputHz:0} Hz -> {output.FrequencyHz:0} Hz : wrong, pain pending";

            // One physical action per tick. This avoids rewarding a blast across
            // every output frequency simultaneously.
            break;
        }
    }

    private void StepPendingConsequences()
    {
        for (int i = pendingConsequences.Count - 1; i >= 0; i--)
        {
            PendingConsequenceAudio10 p = pendingConsequences[i];
            if (p.RemainingTicks > 0)
            {
                p.RemainingTicks--;
                pendingConsequences[i] = p;
                continue;
            }

            if (p.OutputBin >= 0 && p.OutputBin < outputAddresses.Count && p.ChainId != 0)
            {
                Vector3Int address = outputAddresses[p.OutputBin];
                ChemicalCellAudio10 chemical = chemicalField.Probe(address) ?? new ChemicalCellAudio10();
                chemical.ChainId = p.ChainId;
                chemical.Dopamine = Mathf.Max(chemical.Dopamine, p.Dopamine);
                chemical.Pain = Mathf.Max(chemical.Pain, p.Pain);
                chemicalField.Set(address, chemical);

                // Success is satisfying. Wrong attempts only received the small
                // interaction relief above; dopamine provides the large relief.
                if (p.Dopamine > 0f)
                    boredom = Mathf.Max(0f, boredom - p.Dopamine * dopamineBoredomRelief);
            }

            pendingConsequences.RemoveAt(i);
        }
    }

    private void RecordResult(bool correct)
    {
        if (correct) correctOutputs++;
        recentResults.Enqueue(correct);
        if (correct) recentCorrect++;
        while (recentResults.Count > RecentWindow)
        {
            if (recentResults.Dequeue()) recentCorrect--;
        }
    }

    private void RecordFiring(Vector3Int source, Vector3Int target, float signal, ulong chainId)
    {
        if (signal <= 0.001f || chainId == 0 || source == target)
            return;

        float fuel = Mathf.Clamp01(signal * ledgerDeposit);
        Vector3Int forward = target - source;
        Vector3Int backward = source - target;

        LedgerCellAudio10 a = ledgerField.Probe(source) ?? new LedgerCellAudio10();
        UpsertLedger(a.Forward, forward, fuel, chainId);
        ledgerField.Set(source, a);

        LedgerCellAudio10 b = ledgerField.Probe(target) ?? new LedgerCellAudio10();
        UpsertLedger(b.Backward, backward, fuel, chainId);
        ledgerField.Set(target, b);
    }

    private void UpsertLedger(List<LedgerVectorAudio10> list, Vector3Int vector, float fuel, ulong chainId)
    {
        for (int i = 0; i < list.Count; i++)
        {
            LedgerVectorAudio10 e = list[i];
            if (e.Vector != vector || e.ChainId != chainId) continue;
            e.Fuel = Mathf.Max(e.Fuel, fuel); // exact recent evidence, not repeated accumulation
            e.LastUsedTick = tick;
            list[i] = e;
            return;
        }

        list.Add(new LedgerVectorAudio10
        {
            Vector = vector,
            Fuel = fuel,
            ChainId = chainId,
            LastUsedTick = tick
        });
    }

    private void DecayLedger()
    {
        List<KeyValuePair<Vector3Int, LedgerCellAudio10>> snapshot =
            new List<KeyValuePair<Vector3Int, LedgerCellAudio10>>(ledgerField.GetValues());

        for (int n = 0; n < snapshot.Count; n++)
        {
            Vector3Int address = snapshot[n].Key;
            LedgerCellAudio10 ledger = snapshot[n].Value;
            DecayLedgerList(ledger.Forward);
            DecayLedgerList(ledger.Backward);

            if (ledger.Forward.Count == 0 && ledger.Backward.Count == 0)
                ledgerField.Remove(address);
            else
                ledgerField.Set(address, ledger);
        }
    }

    private void DecayLedgerList(List<LedgerVectorAudio10> list)
    {
        for (int i = list.Count - 1; i >= 0; i--)
        {
            LedgerVectorAudio10 e = list[i];
            e.Fuel *= ledgerDecay;
            if (e.Fuel < minimumLedger) list.RemoveAt(i);
            else list[i] = e;
        }
    }

    private void StepChemistry()
    {
        List<KeyValuePair<Vector3Int, ChemicalCellAudio10>> snapshot =
            new List<KeyValuePair<Vector3Int, ChemicalCellAudio10>>(chemicalField.GetValues());
        MutableFieldAudio10<ChemicalCellAudio10> next = new MutableFieldAudio10<ChemicalCellAudio10>();

        for (int n = 0; n < snapshot.Count; n++)
        {
            Vector3Int address = snapshot[n].Key;
            ChemicalCellAudio10 chemical = snapshot[n].Value;
            float dopamine = chemical.Dopamine * chemicalDecay;
            float pain = chemical.Pain * chemicalDecay;
            ulong chainId = chemical.ChainId;

            if (dopamine < minimumChemical) dopamine = 0f;
            if (pain < minimumChemical) pain = 0f;
            if (dopamine <= 0f && pain <= 0f) continue;

            LedgerCellAudio10 ledger = ledgerField.Probe(address);
            if (ledger == null) continue;

            for (int i = 0; i < ledger.Backward.Count; i++)
            {
                LedgerVectorAudio10 inverse = ledger.Backward[i];
                if (inverse.ChainId != chainId || inverse.Fuel < minimumLedger) continue;

                Vector3Int previous = address + inverse.Vector;
                TransformVectorOnContact(previous, address, dopamine, pain, inverse.Fuel);
                AddChemical(next, previous, dopamine, pain, chainId);
            }
        }

        chemicalField = next;
    }

    private void TransformVectorOnContact(
        Vector3Int sourceAddress,
        Vector3Int targetAddress,
        float dopamine,
        float pain,
        float eligibility)
    {
        BrainCellAudio10 source = brainField.Probe(sourceAddress);
        if (source == null) return;

        Vector3Int offset = targetAddress - sourceAddress;
        for (int i = 0; i < source.Vectors.Count; i++)
        {
            BrainVectorAudio10 vector = source.Vectors[i];
            if (vector.Offset != offset) continue;

            float signed = dopamine - pain;
            if (Mathf.Abs(signed) <= 0.0001f) return;

            float amount = Mathf.Max(
                minimumPlasticity,
                Mathf.Abs(signed) * Mathf.Clamp01(eligibility) * vectorPlasticity);

            vector.Weight = Mathf.Clamp(vector.Weight + Mathf.Sign(signed) * amount, 0f, 2f);
            source.Vectors[i] = vector;
            brainField.Set(sourceAddress, source);
            return;
        }
    }

    private void AddChemical(
        MutableFieldAudio10<ChemicalCellAudio10> field,
        Vector3Int address,
        float dopamine,
        float pain,
        ulong chainId)
    {
        ChemicalCellAudio10 c = field.Probe(address) ?? new ChemicalCellAudio10();
        float oldStrength = Mathf.Max(c.Dopamine, c.Pain);
        float newStrength = Mathf.Max(dopamine, pain);

        if (c.ChainId == 0 || c.ChainId == chainId || newStrength >= oldStrength)
        {
            if (c.ChainId != chainId)
            {
                c.Dopamine = 0f;
                c.Pain = 0f;
            }
            c.ChainId = chainId;
            c.Dopamine = Mathf.Max(c.Dopamine, dopamine);
            c.Pain = Mathf.Max(c.Pain, pain);
        }

        field.Set(address, c);
    }

    private void StructuralPlasticity()
    {
        List<KeyValuePair<Vector3Int, BrainCellAudio10>> snapshot =
            new List<KeyValuePair<Vector3Int, BrainCellAudio10>>(brainField.GetValues());

        for (int n = 0; n < snapshot.Count; n++)
        {
            Vector3Int address = snapshot[n].Key;
            BrainCellAudio10 cell = snapshot[n].Value;

            for (int i = cell.Vectors.Count - 1; i >= 0; i--)
            {
                BrainVectorAudio10 vector = cell.Vectors[i];
                if (vector.Age > topologyCheckTicks * 3 && vector.Weight < pruneWeight)
                    cell.Vectors.RemoveAt(i);
            }

            if (cell.Vectors.Count >= maximumVectorsPerCell)
            {
                brainField.Set(address, cell);
                continue;
            }

            float activity = Mathf.Max(cell.Activation, Mathf.Clamp01(Mathf.Abs(cell.Potential)));
            bool frontier = cell.Vectors.Count < 2;
            bool isOrigin = address == origin;

            // Growth hormone does not create brain by itself. Existing living/frontier
            // structure must encounter it. Quiet established interior cells do not sprout.
            if (!isOrigin && !frontier && activity <= 0.05f)
            {
                brainField.Set(address, cell);
                continue;
            }

            float hormone = SampleGrowthHormone(address);
            float boredom01 = maximumBoredom <= 0f ? 0f : Mathf.Clamp01(boredom / maximumBoredom);
            float drivenBackground = Mathf.Clamp01(
                backgroundGrowthChance + boredom01 * boredomGrowthPressure);
            float chance = Mathf.Lerp(drivenBackground, hormoneGrowthChance, Mathf.Clamp01(hormone));

            if (random.NextDouble() > chance)
            {
                brainField.Set(address, cell);
                continue;
            }

            Vector3 gradient = SampleGrowthGradient(address);
            Vector3Int target;

            if (gradient.sqrMagnitude > 0.0001f && random.NextDouble() < growthGradientInfluence)
            {
                Vector3 desired = (Vector3)address + gradient.normalized * growthRadius;
                target = Vector3Int.RoundToInt(desired);
                if (!InsideBrain(target) || target == address)
                    target = RandomNearby(address, growthRadius);
            }
            else
            {
                target = RandomNearby(address, growthRadius);
            }

            Vector3Int offset = target - address;
            if (offset != Vector3Int.zero && !HasVector(cell, offset))
            {
                AddVector(cell, offset, newVectorWeight);
                exploratoryPulses.Add(new PulseAudio10
                {
                    Source = address,
                    Target = target,
                    Strength = exploratoryPulse * (1f +
                        (maximumBoredom <= 0f ? 0f : Mathf.Clamp01(boredom / maximumBoredom)) * boredomPulsePressure),
                    ChainId = cell.ChainId != 0 ? cell.ChainId : currentChainId
                });

                if (growthConsumption > 0f && hormone > 0f)
                {
                    consumedGrowthHormone.TryGetValue(target, out float consumed);
                    consumedGrowthHormone[target] = Mathf.Clamp01(consumed + growthConsumption);
                }
            }

            brainField.Set(address, cell);
        }
    }

    private float SampleGrowthHormone(Vector3 position)
    {
        float concentration = 0f;

        // Real audio can contain many simultaneous bands. Every currently active
        // sensory interface emits hormone proportional to its spectral energy.
        for (int i = 0; i < sensoryAddresses.Count; i++)
        {
            FrequencyPortAudio10 port = sensoryField.Probe(sensoryAddresses[i]);
            if (port == null || port.Value <= 0f) continue;
            concentration = Mathf.Max(concentration,
                SphereHormone(position, sensoryAddresses[i], interfaceGrowthRadius,
                    activeInputGrowthStrength * port.Value));
        }

        // Every output is a persistent possible action interface. None is privileged
        // as the correct answer; they all emit the same weak growth sphere.
        for (int i = 0; i < outputAddresses.Count; i++)
        {
            concentration = Mathf.Max(
                concentration,
                SphereHormone(position, outputAddresses[i], interfaceGrowthRadius, outputGrowthStrength));
        }

        Vector3Int address = Vector3Int.RoundToInt(position);
        if (consumedGrowthHormone.TryGetValue(address, out float consumed))
            concentration *= 1f - Mathf.Clamp01(consumed);

        return Mathf.Clamp01(concentration);
    }

    private static float SphereHormone(Vector3 position, Vector3 center, float radius, float strength)
    {
        if (radius <= 0f || strength <= 0f) return 0f;
        float distance = Vector3.Distance(position, center);
        if (distance >= radius) return 0f;
        float normalized = 1f - distance / radius;
        // Smooth falloff gives the frontier a useful gradient instead of a hard shell.
        return Mathf.Clamp01(normalized * normalized * strength);
    }

    private Vector3 SampleGrowthGradient(Vector3Int address)
    {
        float xp = SampleGrowthHormone(address + Vector3Int.right);
        float xn = SampleGrowthHormone(address + Vector3Int.left);
        float yp = SampleGrowthHormone(address + Vector3Int.up);
        float yn = SampleGrowthHormone(address + Vector3Int.down);
        float zp = SampleGrowthHormone(address + new Vector3Int(0, 0, 1));
        float zn = SampleGrowthHormone(address + new Vector3Int(0, 0, -1));
        return new Vector3(xp - xn, yp - yn, zp - zn);
    }

    private void DecayConsumedGrowthHormone()
    {
        if (consumedGrowthHormone.Count == 0) return;
        List<Vector3Int> keys = new List<Vector3Int>(consumedGrowthHormone.Keys);
        for (int i = 0; i < keys.Count; i++)
        {
            Vector3Int key = keys[i];
            float value = consumedGrowthHormone[key] * 0.90f;
            if (value < 0.01f) consumedGrowthHormone.Remove(key);
            else consumedGrowthHormone[key] = value;
        }
    }

    private Vector3Int StepToward(Vector3Int source, Vector3Int target, float radius)
    {
        Vector3 delta = target - source;
        if (delta.sqrMagnitude <= 0.001f) return source;

        float distance = Mathf.Min(radius, delta.magnitude);
        Vector3 desired = (Vector3)source + delta.normalized * distance;
        Vector3Int result = Vector3Int.RoundToInt(desired);

        if (result == source)
        {
            Vector3Int sign = new Vector3Int(
                Math.Sign(target.x - source.x),
                Math.Sign(target.y - source.y),
                Math.Sign(target.z - source.z));
            result = source + sign;
        }

        return InsideBrain(result) ? result : source;
    }

    private Vector3Int RandomNearby(Vector3Int source, float radius)
    {
        int reach = Mathf.Max(1, Mathf.CeilToInt(radius));
        for (int attempt = 0; attempt < 64; attempt++)
        {
            Vector3Int offset = new Vector3Int(
                random.Next(-reach, reach + 1),
                random.Next(-reach, reach + 1),
                random.Next(-reach, reach + 1));

            if (offset == Vector3Int.zero || offset.magnitude > radius) continue;
            Vector3Int target = source + offset;
            if (InsideBrain(target)) return target;
        }
        return source;
    }

    private bool InsideBrain(Vector3Int p)
    {
        return Mathf.Abs(p.x) <= brainRadius &&
               Mathf.Abs(p.y) <= brainRadius &&
               Mathf.Abs(p.z) <= brainRadius;
    }

    private BrainCellAudio10 GetOrCreateCell(Vector3Int address)
    {
        BrainCellAudio10 cell = brainField.Probe(address);
        if (cell != null) return cell;

        cell = new BrainCellAudio10
        {
            Position = address,
            Vectors = new List<BrainVectorAudio10>()
        };
        brainField.Set(address, cell);
        return cell;
    }

    private void AddVector(BrainCellAudio10 cell, Vector3Int offset, float weight)
    {
        if (offset == Vector3Int.zero || HasVector(cell, offset)) return;
        cell.Vectors.Add(new BrainVectorAudio10
        {
            Offset = offset,
            Weight = Mathf.Clamp(weight, 0f, 2f)
        });
    }

    private static bool HasVector(BrainCellAudio10 cell, Vector3Int offset)
    {
        for (int i = 0; i < cell.Vectors.Count; i++)
            if (cell.Vectors[i].Offset == offset) return true;
        return false;
    }

    private static void AddIncoming(
        Dictionary<Vector3Int, List<SignalAudio10>> incoming,
        Vector3Int address,
        float value,
        ulong chainId)
    {
        if (Mathf.Abs(value) <= 0.001f) return;
        if (!incoming.TryGetValue(address, out List<SignalAudio10> list))
        {
            list = new List<SignalAudio10>();
            incoming[address] = list;
        }
        list.Add(new SignalAudio10 { Strength = value, ChainId = chainId });
    }

    private static void AddCause(
        Dictionary<Vector3Int, List<CauseAudio10>> causes,
        Vector3Int target,
        Vector3Int source,
        float signal,
        ulong chainId)
    {
        if (signal <= 0.001f || chainId == 0) return;
        if (!causes.TryGetValue(target, out List<CauseAudio10> list))
        {
            list = new List<CauseAudio10>();
            causes[target] = list;
        }
        list.Add(new CauseAudio10 { Source = source, Signal = signal, ChainId = chainId });
    }

    private float Range(float minimum, float maximum)
    {
        return minimum + (float)random.NextDouble() * (maximum - minimum);
    }

    private Color? ProbeCellGeometry(Vector3 position)
    {
        Vector3Int address = Vector3Int.RoundToInt(position);
        BrainCellAudio10 cell = brainField.Probe(address);
        FrequencyPortAudio10 sensory = sensoryField.Probe(address);
        FrequencyPortAudio10 output = outputField.Probe(address);
        bool isOrigin = address == origin;

        if (cell == null && sensory == null && output == null && !isOrigin) return null;
        if (Vector3.Distance(position, address) > cellRadius) return null;

        if (isOrigin) return new Color(1f, 0.25f, 1f, 1f);
        if (sensory != null)
        {
            float e = Mathf.Clamp01(sensory.Value);
            return e > 0f
                ? new Color(0.05f, 0.35f + 0.60f * e, 0.55f + 0.45f * e, 1f)
                : new Color(0.08f, 0.25f, 0.45f, 1f);
        }
        if (output != null)
        {
            bool actuallyFired = cell != null && cell.LastFiredTick == tick;
            return actuallyFired
                ? new Color(1f, 0.95f, 0.1f, 1f)
                : new Color(0.30f, 0.24f, 0.06f, 1f);
        }
        if (cell != null && cell.Activation > 0.05f) return new Color(0.1f, 1f, 1f, 1f);
        return new Color(0.38f, 0.38f, 0.38f, 1f);
    }

    private Color? ProbeGrowthHormoneGeometry(Vector3 position)
    {
        float hormone = SampleGrowthHormone(position);
        if (hormone < growthRenderThreshold) return null;

        // White/pearl = growth pressure. This is environmental/developmental
        // chemistry, not electrical activity and not reward chemistry.
        float intensity = Mathf.Clamp01(hormone);
        return new Color(0.65f + 0.35f * intensity, 0.65f + 0.35f * intensity, 0.75f + 0.25f * intensity, 1f);
    }

    private Color? ProbeChemistryGeometry(Vector3 position)
    {
        Vector3Int address = Vector3Int.RoundToInt(position);
        if (Vector3.Distance(position, address) > chemicalRadius) return null;

        ChemicalCellAudio10 c = chemicalField.Probe(address);
        if (c != null && c.Pain > minimumChemical) return new Color(1f, 0.1f, 0.1f, 1f);
        if (c != null && c.Dopamine > minimumChemical) return new Color(0.1f, 1f, 0.2f, 1f);

        LedgerCellAudio10 l = ledgerField.Probe(address);
        if (LedgerFuel(l) > minimumLedger) return new Color(0.72f, 0.2f, 1f, 1f);
        return null;
    }

    private static float LedgerFuel(LedgerCellAudio10 ledger)
    {
        if (ledger == null) return 0f;
        float result = 0f;
        for (int i = 0; i < ledger.Forward.Count; i++) result += ledger.Forward[i].Fuel;
        for (int i = 0; i < ledger.Backward.Count; i++) result += ledger.Backward[i].Fuel;
        return Mathf.Clamp01(result);
    }

    private void Render()
    {
        float size = brainRadius + 1f;
        if (renderCells && cellInterpreter != null)
            cellInterpreter.RenderAroundPoint(Vector3.zero, size, cellRenderGridSize);
        if (renderChemistry && chemistryInterpreter != null)
            chemistryInterpreter.RenderAroundPoint(Vector3.zero, size, chemicalRenderGridSize);
        if (renderGrowthHormone && growthInterpreter != null)
            growthInterpreter.RenderAroundPoint(Vector3.zero, size, growthRenderGridSize);
    }

    private void OnDrawGizmos()
    {
        if (!Application.isPlaying || brainField == null) return;

        if (drawVectorGizmos)
        {
            foreach (var pair in brainField.GetValues())
            {
                Vector3Int source = pair.Key;
                BrainCellAudio10 cell = pair.Value;
                for (int i = 0; i < cell.Vectors.Count; i++)
                {
                    BrainVectorAudio10 v = cell.Vectors[i];
                    bool active = v.LastSignal > 0.01f;
                    if (active) Gizmos.color = Color.white;
                    else
                    {
                        float s = Mathf.Clamp01(v.Weight / 1.25f);
                        Gizmos.color = new Color(s, s, s, Mathf.Lerp(0.10f, 0.65f, s));
                    }
                    Gizmos.DrawLine(transform.TransformPoint(source), transform.TransformPoint(source + v.Offset));
                }
            }
        }

#if UNITY_EDITOR
        if (drawFrequencyLabels)
        {
            for (int i = 0; i < sensoryAddresses.Count; i++)
            {
                UnityEditor.Handles.Label(transform.TransformPoint(sensoryAddresses[i] + Vector3Int.up), $"IN {FrequencyForBin(i):0}Hz");
                UnityEditor.Handles.Label(transform.TransformPoint(outputAddresses[i] + Vector3Int.up), $"OUT {FrequencyForBin(i):0}Hz");
            }
        }
#endif
    }

    private int CountVectors()
    {
        int count = 0;
        foreach (var pair in brainField.GetValues()) count += pair.Value.Vectors.Count;
        return count;
    }

    private int CountActive()
    {
        int count = 0;
        foreach (var pair in brainField.GetValues()) if (pair.Value.Activation > 0.05f) count++;
        return count;
    }

    private float LifetimeAccuracy()
    {
        return outputAttempts == 0 ? 0f : (float)correctOutputs / outputAttempts;
    }

    private float RecentAccuracy()
    {
        return recentResults.Count == 0 ? 0f : (float)recentCorrect / recentResults.Count;
    }

    private void OnGUI()
    {
        if (brainField == null) return;

        GUILayout.BeginArea(new Rect(10, 10, 760, Screen.height - 20), GUI.skin.box);
        GUILayout.Label("MATRIX EXAMPLE 10 — AUDIO FILE / OCTAVE FIELD");
        GUILayout.Label($"Tick {tick:N0} | Cells {brainField.Count:N0} | Active {CountActive():N0} | Vectors {CountVectors():N0}");
        GUILayout.Label("Persistent observer at (0,0,0). No episodes, no waiting for success, no brain reset.");

        if (GUILayout.Button(paused ? "RESUME" : "PAUSE"))
        {
            paused = !paused;
            if (audioSource != null)
            {
                if (paused) audioSource.Pause();
                else if (audioSource.clip != null) audioSource.UnPause();
            }
        }
        if (GUILayout.Button("SINGLE TICK"))
        {
            paused = true;
            if (audioSource != null) audioSource.Pause();
            LifeTick();
        }

        GUILayout.Space(8);
        GUILayout.Label(audioSource != null && audioSource.clip != null ? $"AUDIO: {audioSource.clip.name}  {audioSource.time:0.0}s / {audioSource.clip.length:0.0}s" : "AUDIO: assign an AudioClip");
        GUILayout.Label($"STRONGEST BAND: {(currentInputBin >= 0 ? currentInputHz.ToString("0.0") + " Hz" : "none")}");
        GUILayout.Label("ENVIRONMENT: rewards one-octave-up output; teacher target is not rendered into the brain.");
        GUILayout.Label(lastOutputBin >= 0
            ? $"LAST OUTPUT: {FrequencyForBin(lastOutputBin):0.0} Hz  [bin {lastOutputBin}]"
            : "LAST OUTPUT: none yet");
        GUILayout.Label($"Attempts {outputAttempts:N0} | Correct {correctOutputs:N0} | Accuracy {LifetimeAccuracy() * 100f:0.0}% | Last {recentResults.Count}: {RecentAccuracy() * 100f:0.0}%");
        GUILayout.Label(lastEvent);

        GUILayout.Space(8);
        GUILayout.Label("INPUT ROW — REAL-TIME MP3 SPECTRUM (log frequency):");
        GUILayout.BeginHorizontal();
        for (int i = 0; i < frequencyBinCount; i++)
            GUILayout.Label(sensoryField.Probe(sensoryAddresses[i])?.Value > 0f ? $"[{FrequencyForBin(i):0}]" : $" {FrequencyForBin(i):0} ");
        GUILayout.EndHorizontal();

        GUILayout.Label("OUTPUT ROW:");
        GUILayout.BeginHorizontal();
        for (int i = 0; i < frequencyBinCount; i++)
            GUILayout.Label(i == lastOutputBin ? $"[{FrequencyForBin(i):0}]" : $" {FrequencyForBin(i):0} ");
        GUILayout.EndHorizontal();

        GUILayout.Space(8);
        GUILayout.Label("magenta=observer | bright cyan=active audio bands | dark blue=sensory bins | yellow=ACTUAL output firing | cyan=active brain | white=growth hormone | purple=ledger | green=dopamine | red=pain");
        GUILayout.EndArea();
    }
}

public sealed class BrainCellAudio10
{
    public Vector3Int Position;
    public float Potential;
    public float Activation;
    public long LastFiredTick;
    public ulong ChainId;
    public List<BrainVectorAudio10> Vectors = new();
}

public struct BrainVectorAudio10
{
    public Vector3Int Offset;
    public float Weight;
    public float LastSignal;
    public long Age;
}

public sealed class LedgerCellAudio10
{
    public List<LedgerVectorAudio10> Forward = new();
    public List<LedgerVectorAudio10> Backward = new();
}

public struct LedgerVectorAudio10
{
    public Vector3Int Vector;
    public float Fuel;
    public ulong ChainId;
    public long LastUsedTick;
}

public sealed class ChemicalCellAudio10
{
    public float Dopamine;
    public float Pain;
    public ulong ChainId;
}

public sealed class FrequencyPortAudio10
{
    public int Bin;
    public float FrequencyHz;
    public float Value;
}

public struct SignalAudio10
{
    public ulong ChainId;
    public float Strength;
}

public struct CauseAudio10
{
    public Vector3Int Source;
    public float Signal;
    public ulong ChainId;
}

public struct PulseAudio10
{
    public Vector3Int Source;
    public Vector3Int Target;
    public float Strength;
    public ulong ChainId;
}

public struct PendingConsequenceAudio10
{
    public int RemainingTicks;
    public int InputBin;
    public int OutputBin;
    public int ExpectedBin;
    public ulong ChainId;
    public float Dopamine;
    public float Pain;
}

public sealed class MutableFieldAudio10<T> : IFieldPopulator<Vector3, T> where T : class
{
    private readonly Dictionary<Vector3Int, T> values = new();
    public int Count => values.Count;

    public T Probe(Vector3 position, T defaultValue)
    {
        Vector3Int address = Vector3Int.RoundToInt(position);
        return values.TryGetValue(address, out T value) ? value : defaultValue;
    }

    public T Probe(Vector3 position) => Probe(position, null);

    public void Set(Vector3 position, T value)
    {
        Vector3Int address = Vector3Int.RoundToInt(position);
        if (value == null) values.Remove(address);
        else values[address] = value;
    }

    public void Remove(Vector3 position) => values.Remove(Vector3Int.RoundToInt(position));
    public IEnumerable<KeyValuePair<Vector3Int, T>> GetValues() => values;
}