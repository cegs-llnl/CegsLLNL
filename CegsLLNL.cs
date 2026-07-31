using AeonHacs.Wpf.Views;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static AeonHacs.Components.CegsPreferences;
using static AeonHacs.Notify;
using static AeonHacs.Utilities.Utility;

namespace AeonHacs.Components;

public partial class CegsLLNL : Cegs
{
    #region HacsComponent

    [HacsConnect]
    protected override void Connect()
    {
        base.Connect();

        SampleRecords = Find<HacsLog>("SampleRecords");
        ChamberCT1 = Find<IChamber>("CT1");
        IP1 = Find<InletPort>("IP1");
        //        IP2 = Find<InletPort>("IP2");

        // Sections
        CA = Find<Section>("CA");
        CTF = Find<Section>("CTF");
        CT1 = Find<Section>("CT1");
        CT2 = Find<Section>("CT2");

        FTG_IP1 = Find<Section>("FTG_IP1");

        IM_CT1 = Find<Section>("IM_CT1");
        IM_CT2 = Find<Section>("IM_CT2");
        IM_CA_CT1 = Find<Section>("IM_CA_CT1");
        IM_CA_CT2 = Find<Section>("IM_CA_CT2");

        CA1 = Find<SableCA10>("CA1");
        mfcHe = Find<ManagedMFC>("mfcHe");
        mfcO2 = Find<ManagedMFC>("mfcO2");

        CtFlowMonitor = Find<FlowMonitor>("CtFlowMonitor");
        CollectedUgc = Find<Meter>("CollectedUgc");

        // Select the default Coil Trap
        SelectCT1();
    }

    [HacsPostConnect]
    protected override void PostConnect()
    {
        base.PostConnect();
        CA1.PropertyChanged += UpdateCollectedCO2;
        CtFlowMonitor.PropertyChanged += UpdateCollectedCO2;

        mfcHe.PropertyChanged += UpdateFlowRate;
        mfcO2.PropertyChanged += UpdateFlowRate;
    }

    #endregion HacsComponent

    #region System configuration

    #region HacsComponents

    IChamber ChamberCT1 { get; set; }
    InletPort IP1 { get; set; }
    public virtual HacsLog SampleRecords { get; set; }

    #region Sections

    /// <summary>
    /// The CT section of the sample collection path IM_FirstTrap.
    /// </summary>
    public override ISection CT => FirstTrap;

    /// <summary>
    /// The sample gas collection path; one of IM_CT1, IM_CT2, IM_CA_CT1, IM_CA_CT2;
    /// </summary>
    protected override ISection IM_FirstTrap { get => base.IM_FirstTrap; set => base.IM_FirstTrap = value; }

    /// <summary>
    /// CO2 Analyzer section
    /// </summary>
    public ISection CA { get; set; }

    /// <summary>
    /// Coil Trap Flow section
    /// </summary>
    public ISection CTF { get; set; }

    /// <summary>
    /// Coil Trap 1 section
    /// </summary>

    public ISection CT1 { get; set; }

    /// <summary>
    /// Coil Trap 2 section
    /// </summary>
    public ISection CT2 { get; set; }

    /// <summary>
    /// Flow-Through Gas section
    /// </summary>
    public ISection FTG { get; set; }

    /// <summary>
    /// Inlet Port 1 section
    /// </summary>
//    public ISection IP1 { get; set; }

    /// <summary>
    /// Flow-Through Gas..Inlet Port 1 section
    /// </summary>
    public ISection FTG_IP1 { get; set; }

    /// <summary>
    /// Flow-Through Gas..Inlet Manifold section
    /// </summary>
    public ISection FTG_IM { get; set; }

    /// <summary>
    /// Flow-Through Gas..Carbon Analyzer section
    /// </summary>
    public ISection FTG_CA { get; set; }

    /// <summary>
    /// Inlet Manifold..Coil Trap Flow section (bypasses CO2 analyzer)
    /// </summary>
//    public ISection IM_CTF { get; set; }

    /// <summary>
    /// Inlet Manifold..CO2 Analyzer..Coil Trap Flow section
    /// </summary>
//    public ISection IM_CA_CTF { get; set; }

    /// <summary>
    /// Inlet Manifold..Coil Trap 1 section (bypasses CO2 Analyzer)
    /// </summary>
    public ISection IM_CT1 { get; set; }

    /// <summary>
    /// Inlet Manifold..Coil Trap 2 section (bypasses CO2 Analyzer)
    /// </summary>
    public ISection IM_CT2 { get; set; }

    /// <summary>
    /// Inlet Manifold..CO2 analyzer..Coil Trap 1 section
    /// </summary>
    public ISection IM_CA_CT1 { get; set; }

    /// <summary>
    /// Inlet Manifold..CO2 analyzer..Coil Trap 2 section
    /// </summary>
    public ISection IM_CA_CT2 { get; set; }

    #endregion Sections

    /// <summary>
    /// CO2 analyzer
    /// </summary>
    public SableCA10 CA1 { get; set; }

    /// <summary>
    /// Ambient air pressure.
    /// </summary>
    public IManometer pAmbient => Ambient.Manometer;

    /// <summary>
    /// Mass Flow Controller for He to FTG section
    /// </summary>
    public ManagedMFC mfcHe { get; set; }

    /// <summary>
    /// Mass Flow Controller for O2 to FTG section
    /// </summary>
    public ManagedMFC mfcO2 { get; set; }

    public FlowMonitor CtFlowMonitor { get; set; }
    public Meter CollectedUgc { get; set; }

    #endregion HacsComponents

    #endregion System configuration

    #region Periodic system activities & maintenance

    protected override void ZeroPressureGauges() => ZeroPressureGauges([MC, CTF, IM, GM, .. GraphiteReactors]);

    #endregion Periodic system activities & maintenance

    #region Process Management

    protected override void BuildProcessDictionary()
    {
        Separators.Clear();

        // Running samples
        ProcessDictionary["Run samples"] = RunSamples;
        Separators.Add(ProcessDictionary.Count);

        // Preparation for running samples
        ProcessDictionary["Prepare GRs for new iron and desiccant"] = PrepareGRsForService;
        ProcessDictionary["Precondition GR iron"] = PreconditionGRs;
        ProcessDictionary["Replace iron in sulfur traps"] = ChangeSulfurFe;
        ProcessDictionary["Prepare active inlet port"] = PrepareInletPort;
        ProcessDictionary["Prepare loaded inlet ports for collection"] = PrepareInletPorts;
        Separators.Add(ProcessDictionary.Count);

        // d13C ports prep
        ProcessDictionary["Reload completed d13C ports"] = Reload_d13CPorts;
        Separators.Add(ProcessDictionary.Count);

        // carbonate sample prep
        ProcessDictionary["Prepare carbonate sample for acid"] = PrepareCarbonateSample;
        ProcessDictionary["Load acidified carbonate sample"] = LoadCarbonateSample;
        Separators.Add(ProcessDictionary.Count);

        // Open line
        ProcessDictionary["Open and evacuate line"] = OpenLine;
        ProcessDictionary["Open and evacuate line (IM)"] = OpenLineIM;
        ProcessDictionary["Open and evacuate line (MC)"] = OpenLineMC;
        Separators.Add(ProcessDictionary.Count);

        // Main process continuations
        ProcessDictionary["Collect, etc."] = CollectEtc;
        ProcessDictionary["Transfer CO2 to VTT, etc."] = TransferCO2FromCTToVttEtc;
        ProcessDictionary["Extract, etc."] = ExtractEtc;
        ProcessDictionary["Measure, etc."] = MeasureEtc;
        ProcessDictionary["Graphitize, etc."] = GraphitizeEtc;
        Separators.Add(ProcessDictionary.Count);

        // Top-level steps for standard protocol
        ProcessDictionary["Admit sealed CO2 to InletPort"] = AdmitSealedCO2IP;
        ProcessDictionary["Collect CO2 from InletPort"] = Collect;
        ProcessDictionary["Transfer CO2 from CT to VTT"] = TransferCO2FromCTToVTT;
        ProcessDictionary["Extract"] = Extract;
        ProcessDictionary["Measure"] = Measure;
        ProcessDictionary["Discard excess CO2 by splits"] = DiscardSplit;
        ProcessDictionary["Remove sulfur"] = RemoveSulfur;
        ProcessDictionary["Dilute small sample"] = Dilute;
        ProcessDictionary["Graphitize aliquots"] = GraphitizeAliquots;
        ProcessDictionary["Add d13C carrier"] = AddCarrierTo_d13C;
        Separators.Add(ProcessDictionary.Count);

        // Secondary-level process sub-steps
        ProcessDictionary["Evacuate Inlet Port"] = EvacuateIP;
        ProcessDictionary["Flush Inlet Port"] = FlushIP;
        ProcessDictionary["Admit O2 into Inlet Port"] = AdmitIPO2;
        ProcessDictionary["Heat quartz media"] = HeatQuartz;
        ProcessDictionary["Heat Quartz and Open Line"] = HeatQuartzOpenLine;
        ProcessDictionary["Turn off IP furnaces"] = TurnOffIPFurnaces;
        ProcessDictionary["Discard IP gases"] = DiscardIPGases;
        ProcessDictionary["Close IP"] = CloseIP;
        ProcessDictionary["Isolate IP"] = IsolateIP;
        ProcessDictionary["Prepare for collection"] = PrepareForCollection;
        ProcessDictionary["Start collecting"] = StartCollecting;
        ProcessDictionary["Clear collection conditions"] = ClearCollectionConditions;
        ProcessDictionary["Collect until condition met"] = CollectUntilConditionMet;
        ProcessDictionary["Stop collecting"] = StopCollecting;
        ProcessDictionary["Stop collecting immediately"] = StopCollectingImmediately;
        ProcessDictionary["Stop collecting after bleed down"] = StopCollectingAfterBleedDown;
        ProcessDictionary["Evacuate and Freeze first trap"] = FreezeFirstTrap;
        ProcessDictionary["Evacuate and Freeze VTT"] = FreezeVtt;
        ProcessDictionary["Admit Dead CO2 into MC"] = AdmitDeadCO2;
        ProcessDictionary["Purify CO2 in MC"] = CleanupCO2InMC;
        ProcessDictionary["Discard MC gases"] = DiscardMCGases;
        ProcessDictionary["Divide sample into aliquots"] = DivideAliquots;
        Separators.Add(ProcessDictionary.Count);

        // Split sample processing
        ProcessDictionary["Create a sample split"] = CreateSampleSplit;
        ProcessDictionary["Wait for CEGS to be free"] = WaitForCegs;
        ProcessDictionary["Launch Transfer, etc."] = LaunchTransferEtc;
        ProcessDictionary["Collect sample gas, then launch Transfer, etc."] = CollectAndLaunchTransferEtc;
        ProcessDictionary["Keep all LN Manifolds Active"] = KeepAllLNManifoldsActive;
        ProcessDictionary["Resume all LN Manifolds Monitoring"] = ResumeAllLNManifoldsMonitoring;
        ProcessDictionary["Open and Evacuate VTT..GM"] = OpenAndEvacuateVttToGM;
        Separators.Add(ProcessDictionary.Count);

        // Granular inlet port & sample process control
        ProcessDictionary["Reset Inlet Port to Loaded"] = ResetIpToLoaded;
        ProcessDictionary["Freeze the Inlet Port"] = FreezeIp;
        ProcessDictionary["Raise LN on the Inlet Port"] = RaiseLNIp;
        ProcessDictionary["Thaw the Inlet Port"] = ThawIp;
        ProcessDictionary["Raise IP furnaces"] = RaiseIpFurnaces;
        ProcessDictionary["Turn on quartz furnace"] = TurnOnIpQuartzFurnace;
        ProcessDictionary["Turn off quartz furnace"] = TurnOffIpQuartzFurnace;
        ProcessDictionary["Turn on sample furnace"] = TurnOnIpSampleFurnace;
        ProcessDictionary["Wait for sample to rise to setpoint"] = WaitIpRiseToSetpoint;
        ProcessDictionary["Wait for sample to fall to setpoint"] = WaitIpFallToSetpoint;
        ProcessDictionary["Turn off sample furnace"] = TurnOffIpSampleFurnace;
        Separators.Add(ProcessDictionary.Count);

        // General-purpose process control actions
        ProcessDictionary["Wait for timer"] = WaitForTimer;
        ProcessDictionary["Wait for IP timer"] = WaitIpMinutes;
        ProcessDictionary["Wait for operator"] = Notify.WaitForOperator;
        Separators.Add(ProcessDictionary.Count);

        // Transferring CO2
        ProcessDictionary["Transfer CO2 from MC to VTT"] = TransferCO2FromMCToVTT;
        ProcessDictionary["Transfer CO2 from MC to GR"] = TransferCO2FromMCToGR;
        ProcessDictionary["Transfer CO2 from prior GR to MC"] = TransferCO2FromGRToMC;
        Separators.Add(ProcessDictionary.Count);

        // Flow control steps & special collection operations
        ProcessDictionary["Reset tracked flow and collected µgC"] = ResetUgcTracking;
        ProcessDictionary["Select CT1"] = SelectCT1;
        ProcessDictionary["Select CT2"] = SelectCT2;
        ProcessDictionary["Toggle CT collection"] = ToggleCT;
        Separators.Add(ProcessDictionary.Count);


        // Flow control sub-steps
        ProcessDictionary["Start flow through to trap"] = StartFlowThroughToTrap;
        ProcessDictionary["Start flow through to waste"] = StartFlowThroughToWaste;
        ProcessDictionary["Stop flow-through gas"] = StopFtg;
        Separators.Add(ProcessDictionary.Count);

        // d13C port service routines
        //ProcessDictionary["Empty completed d13C ports"] = EmptyCompleted_d13CPorts;
        //ProcessDictionary["Thaw frozen d13C ports"] = ThawFrozen_d13CPorts;
        //ProcessDictionary["Load empty d13C ports"] = LoadEmpty_d13CPorts;
        ProcessDictionary["Prepare loaded d13C ports"] = PrepareLoaded_d13CPorts;
        Separators.Add(ProcessDictionary.Count);


        // Utilities (generally not for sample processing)
        ProcessDictionary["Exercise all Opened valves"] = ExerciseAllValves;
        ProcessDictionary["Close all Opened valves"] = CloseAllValves;
        ProcessDictionary["Exercise all LN Manifold valves"] = ExerciseLNValves;
        ProcessDictionary["Close all LN Manifold valves"] = CloseLNValves;
        ProcessDictionary["Calibrate all multi-turn valves"] = CalibrateRS232Valves;
        ProcessDictionary["Open all multi-turn valves"] = OpenRS232Valves;
        ProcessDictionary["Measure MC volume (KV in MCP1)"] = MeasureVolumeMC;
        ProcessDictionary["Measure valve volumes (plug in MCP1)"] = MeasureValveVolumes;
        ProcessDictionary["Measure remaining chamber volumes"] = MeasureRemainingVolumes;
        ProcessDictionary["Check GR H2 density ratios"] = CalibrateGRH2;
        //ProcessDictionary["Calibrate VP N2 initial manifold pressure"] = CalibrateVPHeP0;
        ProcessDictionary["Measure Extraction efficiency"] = MeasureExtractEfficiency;
        ProcessDictionary["Measure IP collection efficiency"] = MeasureIpCollectionEfficiency;
        Separators.Add(ProcessDictionary.Count);

        // Test functions
        ProcessDictionary["Test"] = Test;
    }

    #region OpenLine

    /// <summary>
    /// Open and evacuate the entire vacuum line. This establishes
    /// the baseline system state: the condition it is normally left in
    /// when idle, and the expected starting point for major
    /// processes such as running samples.
    /// </summary>
    protected override void OpenLine()
    {
        var step = ProcessStep.Start("Close gas supplies");
        CloseGasSupplies();
        step.End();

        var vacuumSystems = VacuumSystems.Values.ToList();
        vacuumSystems.ForEach(OpenLine);

        step = ProcessStep.Start($"Wait for all vacuum systems to reach {OkPressure} Torr");
        WaitFor(() => vacuumSystems.All(vs => vs.Pressure <= OkPressure));
        step.End();

        step = ProcessStep.Start("Join vacuum system lines");
        // compute all pairs?
        Section.Connections(vacuumSystems.First().MySection, vacuumSystems.Last().MySection).Open();
        step.End();

        step = ProcessStep.Start($"Isolate {CA.Name} (temp. due to leak)");
        CA.Isolate();
        step.End();

    }

    #endregion OpenLine

    /// <summary>
    /// Whenever the MC sample measurement (in ugC) changes,
    /// notify subscribers that umolCinMC has changed as well.
    /// </summary>
    protected override void UpdateSampleMeasurement(object sender = null, PropertyChangedEventArgs e = null)
    {
        var ugC = ugCinMC.Value;
        base.UpdateSampleMeasurement(sender, e);
        if (ugCinMC.Value != ugC)
            NotifyPropertyChanged(nameof(umolCinMC));
    }

    #region Process Control Parameters

    /// <summary>
    /// "Provide a flow of oxygen through the Inlet Port to combust the sample, instead of a fixed pressure.
    /// </summary>
    public bool FlowThroughIP => ParameterTrue(nameof(FlowThroughIP));

    /// <summary>
    /// Amount of He to flow through IP, in sccm
    /// </summary>
    public double FlowThroughHe => GetParameter(nameof(FlowThroughHe));

    /// <summary>
    /// Amount of O2 to flow through IP, in sccm
    /// 
    public double FlowThroughO2 => GetParameter(nameof(FlowThroughO2));


    /// <summary>
    /// Whether or not to include the CO2 analyzer in the collection path.
    /// </summary>
    public bool IncludeCO2Analyzer => ParameterTrue(nameof(IncludeCO2Analyzer));

    /// <summary>
    /// Stop collecting into the coil trap when amount of carbon in the Coil Trap reaches this value,
    /// provided that it is a number (i.e., not NaN).
    /// </summary>
    public double CollectUntilUgc => GetParameter("CollectUntilUgc");

    #endregion Process Control Parameters

    #region Process Control Properties

    /// <summary>
    /// The coil trap currently being used to trap sample gas.
    /// </summary>
    public ISection CurrentCT => IM_FirstTrap.Chambers.Contains(ChamberCT1) ? CT1 : CT2;

    /// <summary>
    /// A CEGS task dispatched to run concurrently while the main 
    /// sample process sequence continues. There can be only one.
    /// The concurrent actions take place in the VTT or beyond.
    /// </summary>
    public Task CegsTask { get; set; }

    /// <summary>
    /// A Collection task dispatched to run concurrently while
    /// the main process sequence continues. There can be only one.
    /// </summary>

    public Task CollectionTask { get; set; }

    #endregion Process Control Properties

    #region Process Steps

    /// <summary>
    /// Evacuate the Inlet Port to 'OkPressure'.
    /// </summary>
    protected override void EvacuateIP()
    {
        var step = ProcessStep.Start($"Evacuate {InletPort.Name}");

        if (FlowThroughIP && InletPort == IP1)
            FTG_IP1.Open();
        base.EvacuateIP(IpEvacuationPressure);

        step.End();
    }

    protected override void EvacuateAndCheckIPs(ISection im, IEnumerable<IPort> ips)
    {
        if (FlowThroughIP && ips.Contains(IP1))
            FTG_IP1.Open();
        base.EvacuateAndCheckIPs(im, ips);
    }


    /// <summary>
    /// Start flowing gas through the Inlet Port, analyzer, and the (warm) coil trap to vacuum.
    /// </summary>
    protected virtual void StartFlowThroughToWaste() => StartFlowThrough(false);

    /// <summary>
    /// Start flowing O2 through the Inlet Port and the frozen coil trap.
    /// </summary>
    protected virtual void StartFlowThroughToTrap() => StartFlowThrough(true);

    /// <summary>
    /// Start flowing O2 through the Inlet Port.
    /// </summary>
    protected virtual void StartFlowThrough(bool trap)
    {
        var step = ProcessStep.Start($"Start flowing O2 through {InletPort.Name}");

        var source = FTG_IP1;

        var substep = ProcessSubStep.Start($"Isolate and open section {source.Name}.");
        source.Isolate();
        source.Open();
        substep.End();

        // TODO: These should already be off...remove?
        //mfcHe.TurnOff();
        //mfcO2.TurnOff();

        IM_FirstTrap.FlowManager.StopOnFullyOpened = false;
        //StartSampleFlow(trap);          // Manage CT flow to maintain bleed pressure
        StartCollecting();              // Manage CT flow to maintain bleed pressure
        StartFtg();
        step.End();
    }

    /// <summary>
    /// Start flowing the desired gas mixture into the Inlet Port.
    /// </summary>
    public virtual void StartFtg()
    {
        if (FlowThroughHe.IsANumber() && FlowThroughHe > 0)
        {
            mfcHe.TurnOn(FlowThroughHe);
            WaitFor(() => Math.Abs(mfcHe.FlowRate - GetParameter("FlowThroughHe")) < 0.1, -1, 1000);
        }

        if (FlowThroughO2.IsANumber() && FlowThroughO2 > 0)
        {
            mfcO2.TurnOn(GetParameter("FlowThroughO2"));
            WaitFor(() => Math.Abs(mfcO2.FlowRate - GetParameter("FlowThroughO2")) < 0.1, -1, 1000);
        }
    }

    /// <summary>
    /// Stop flowing gas into the Inlet Port.
    /// </summary>
    protected virtual void StopFtg()
    {
        var step = ProcessStep.Start($"Stopping gas flow into {InletPort.Name}");
        mfcHe.TurnOff();
        mfcO2.TurnOff();
        step.End();
    }

    /// <summary>
    /// Use Coil Trap 1 for sample collection;
    /// </summary>
    protected virtual void SelectCT1() => base.IM_FirstTrap =
        IncludeCO2Analyzer ? IM_CA_CT1 : IM_CT1;

    /// <summary>
    /// Use Coil Trap 2 for sample collection.
    /// </summary>
    protected virtual void SelectCT2() => base.IM_FirstTrap =
        IncludeCO2Analyzer ? IM_CA_CT2 : IM_CT2;

    /// <summary>
    /// Stop the flow-through gas if it's going and stop collecting CO2
    /// </summary>
    protected override void StopCollecting(bool immediately = true)
    {
        if (mfcO2.IsOn || mfcHe.IsOn) StopFtg();
        var valves = IM_FirstTrap?.InternalValves;
        var count = valves?.Count ?? 0;
        if (count > 1)  // next-to-last internal valve feeds vCTFlow
            valves[count - 2].CloseWait();
        base.StopCollecting(immediately);
    }

    /// <summary>
    /// Switch coil traps.
    /// </summary>
    protected virtual void ToggleCT()
    {
        var step = ProcessStep.Start($"Toggle CT");

        if (FirstTrap == CT1)
            SelectCT2();
        else
            SelectCT1();

        step.End();
    }

    protected void ResetUgcTracking()
    {
        CtFlowMonitor.Reset();
        CollectedUgc.Update(0);
        ugCTrackingStopwatch.Restart();
    }

    protected override void StartCollecting()
    {
        ResetUgcTracking();
        base.StartCollecting();
    }

    /// <summary>
    /// Set all collection stop condition parameters to NaN
    /// </summary>
    protected override void ClearCollectionConditions()
    {
        base.ClearCollectionConditions();
        ClearParameter("CollectUntilUgc");
    }

    protected override List<Func<string>> CollectionConditions()
    {
        IpIm(out ISection im);
        double p;

        var CollectionConditions = base.CollectionConditions();

        p = CollectUntilUgc;
        if (p.IsANumber())
        {
            var value = p;
            CollectionConditions.Add(() => CollectedUgc >= value ?
                $"Collected >= {CollectUntilUgc:0} µg C" : "");
        }

        return CollectionConditions;
    }


    /// <summary>
    /// To torch them off
    /// </summary>
    protected void FreezeCompleted_d13CPorts()
    {
        var ports = d13CPorts.FindAll(p => p.State == LinePort.States.Complete);
        var step = ProcessStep.Start("Freeze completed d13C ports");
        ports.ForEach(p => p.Coldfinger.Freeze());
        WaitFor(() => ports.All(p => p.Coldfinger.Frozen));
        step.End();
    }

    /// <summary>
    /// After torch-off
    /// </summary>
    protected void ThawFrozen_d13CPorts()
    {
        var ports = d13CPorts.FindAll(p => p.Coldfinger.IsActivelyCooling);
        ports.ForEach(p => p.Coldfinger.Thaw());
    }

    /// <summary>
    /// Mark the completed ports empty.
    /// </summary>
    protected void EmptyCompleted_d13CPorts()
    {
        var ports = d13CPorts.FindAll(p => p.State == LinePort.States.Complete);
        ports.ForEach(p => p.State = LinePort.States.Empty);
    }

    /// <summary>
    /// Mark the Empty ports loaded.
    /// </summary>
    protected void LoadEmpty_d13CPorts()
    {
        var ports = d13CPorts.FindAll(p => p.State == LinePort.States.Empty);
        ports.ForEach(p => p.State = LinePort.States.Loaded);
    }

    /// <summary>
    /// Remove and replace d13C ampoules
    /// </summary>
    protected void Service_d13CPorts()
    {
        var ports = d13CPorts.FindAll(p => p.State == LinePort.States.Complete);
        if (ports.Count > 0)
        {
            FreezeCompleted_d13CPorts();
            WaitForOperator($"Torch off the completed d13C splits.");
            EmptyCompleted_d13CPorts();
        }
        ThawFrozen_d13CPorts();
        ports = d13CPorts.FindAll(p => p.State == LinePort.States.Empty);
        if (ports.Count > 0)
        {
            WaitForOperator("Load new ampoules into the empty ports.");
            LoadEmpty_d13CPorts();
        }
        PrepareLoaded_d13CPorts();
    }


    StringBuilder sampleRecord = new StringBuilder();
    /// <summary>
    /// Record the Sample data in LLNL's preferred format
    /// </summary>
    /// <param name="aliquot"></param>
    protected override void SampleRecord(Aliquot aliquot)
    {
        if (aliquot == null) return;

        var gr = Find<IGraphiteReactor>(aliquot.GraphiteReactor);
        if (gr == null || IsSulfurTrap(gr)) return;

        var sample = aliquot.Sample;

        var grPressure = gr.Pressure;       // Torr
        var grTemperature = gr.SampleTemperature;
        var grMilliLiters = gr.MilliLiters;

        var nTotalC = sample.TotalMicrogramsCarbon * CarbonAtomsPerMicrogram;  // total number of carbon atoms in the sample
        var TorrMC = Pressure(nTotalC, MC.MilliLiters, MC.Temperature);
        var PercentC = 100 * sample.TotalMicrogramsCarbon / sample.Micrograms;
        var nCO2 = aliquot.MicrogramsCarbon * CarbonAtomsPerMicrogram;  // number of CO2 particles in the aliquot
        var nH2 = nCO2 * aliquot.H2CO2PressureRatio;    // H2 particles introduced
        var TorrCO2 = Pressure(nCO2, gr.MilliLiters, grTemperature);  // Torr
        var TorrH2 = Pressure(nH2, gr.MilliLiters, grTemperature);  // Torr
        var TorrTotalExp = Pressure(nCO2 + nH2, gr.MilliLiters, grTemperature);  // Torr
        var TorrTotalMeas = aliquot.GRStartPressure;
        var kelvins = grTemperature + ZeroDegreesC;
        var TorrResExp = aliquot.ExpectedResidualPressure * kelvins;
        var TorrRes = aliquot.ResidualMeasured ? aliquot.ResidualPressure * kelvins : grPressure;   // Torr

        var excessH2Particles = nH2 - H2_CO2StoichiometricRatio * nCO2; // introduced
        var residualParticles = Particles(TorrRes, grMilliLiters, grTemperature);
        var residualCO2Particles = (residualParticles - excessH2Particles) / 3;
        var graphitizationYield = 100 * (nCO2 - residualCO2Particles) / nCO2;

        sampleRecord.Append($"{sample.DateTime:yyyy-MM-dd HH:mm:ss}");
        sampleRecord.Append($"\t{sample.LabId}");
        sampleRecord.Append($"\t{sample.Milligrams}");
        sampleRecord.Append($"\t{sample.InletPort.Name}");
        sampleRecord.Append($"\t{sample.Traps}");    //first trap, usually CT, CT1 or CT2
        sampleRecord.Append($"\t{sample.TotalMicrogramsCarbon:0.0}"); // TCO2
        sampleRecord.Append($"\t{TorrMC:0.00}");
        sampleRecord.Append($"\t{PercentC:0.00}");
        sampleRecord.Append($"\t{sample.Discards}");
        sampleRecord.Append($"\t{sample.SelectedMicrogramsCarbon:0.0}");
        sampleRecord.Append($"\t{sample.Micrograms_d13C:0.0}");
        sampleRecord.Append($"\t{sample.d13CPort?.Name ?? ""}");
        sampleRecord.Append($"\t{aliquot.GraphiteReactor}");
        sampleRecord.Append($"\t{aliquot.Name}");
        sampleRecord.Append($"\t{aliquot.MicrogramsCarbon:0.0}");
        sampleRecord.Append($"\t{TorrCO2:0}");
        sampleRecord.Append($"\t{TorrH2:0}");
        sampleRecord.Append($"\t{TorrTotalExp:0}");
        sampleRecord.Append($"\t{TorrTotalMeas:0}");
        sampleRecord.Append($"\t{TorrResExp:0}");
        sampleRecord.Append($"\t{TorrRes:0}");

        SampleRecords.WriteLine(sampleRecord.ToString());
        sampleRecord.Clear();
    }


    #endregion Process Steps

    #endregion Process Management

    protected virtual void FtgPressurize(string gasName, ISection destination, double pressure)
    {
        var mfc =
            gasName == "He" ? mfcHe :
            gasName == "O2" ? mfcO2 :
            default;

        // TODO handle errors (Notify?)
        if (mfc == default) return;
        if (!destination.Chambers.Contains(Find<Chamber>("FTG"))) return; // don't know how to get there

        destination.OpenAndEvacuate();
        destination.Isolate();

        mfc.TurnOn(mfc.MaximumSetpoint);            // add Pressurize(Meter, double) to mfc class
        WaitFor(() => destination.Pressure >= pressure);
        mfc.TurnOff();
    }

    #region Coil trap flow rate testing and estimation

    Stopwatch ugCTrackingStopwatch = new Stopwatch();
    double Co2Percent;
    double CtSccm;
    protected virtual void UpdateCollectedCO2(object sender, PropertyChangedEventArgs e)
    {
        void update()
        {
            var minutes = ugCTrackingStopwatch.Elapsed.TotalMinutes;
            ugCTrackingStopwatch.Restart();
            var sccmCo2 = CtSccm * minutes * Co2Percent / 100;
            var ugc = CollectedUgc + Particles(Torr, sccmCo2, MC.Temperature) / CarbonAtomsPerMicrogram;
            CollectedUgc.Update(ugc);
        }

        var property = e.PropertyName;
        if (sender == CA1 && property == nameof(CA1.CO2Percent) && Co2Percent != CA1.CO2Percent)
        {
            update();
            Co2Percent = CA1.CO2Percent;
        }
        else if (sender == CtFlowMonitor && property == nameof(CtFlowMonitor.FlowRate) && CtSccm != CtFlowMonitor.FlowRate)
        {
            update();
            CtSccm = CtFlowMonitor.FlowRate;
        }
    }

    protected virtual void UpdateFlowRate(object sender, PropertyChangedEventArgs e)
    {
        var property = e.PropertyName;
        if (sender is IMassFlowController && property == nameof(IMassFlowController.FlowRate))
            CtFlowMonitor.FlowRateMeter.Update(mfcHe.FlowRate + mfcO2.FlowRate);
    }


    protected virtual (double, double) CTFlowTest(double pressure, int position)
    {
        var dest = Find<Section>("IM_CA");
        var vFlow = IM_CA_CT1.FlowManager.FlowValve;

        var step = ProcessStep.Start("Evacuate IM_CA_CT1");
        IM_CA_CT1.Isolate();
        vFlow.Open();
        IM_CA_CT1.OpenAndEvacuate(0.005);
        vFlow.Close();
        step.End();

        FtgPressurize("O2", dest, pressure);

        step = ProcessStep.Start("Bleed IM_CA down until pCT < 0.5 Torr");
        IM_CA_CT1.OpenAndEvacuate();
        vFlow.MoveTo(position);
        WaitSeconds(5);
        var p0 = IM.Pressure;
        WaitFor(() => CTF.Pressure < 0.5);
        var p1 = IM.Pressure;
        step.End();

        return (p0, p1);
    }

    protected virtual void CTFlowTestSequence()
    {
        //var testPressure = 500.0;
        //var testPosition = 660;
        var testPressure = 300.0;
        var testPosition = 647;
        while (testPressure > 5 && testPosition >= 0)
        {
            var pa = CTFlowTest(testPressure, testPosition);
            var pHigh = pa.Item1;
            var pLow = pa.Item2;
            testPressure = Math.Min(pHigh, pLow * 1.4);
            var dPos = (int)((672 - testPosition) * 1.1);
            testPosition = Math.Max(0, testPosition - dPos);
        }
    }

    #endregion Coil trap flow rate testing and estimation

    protected override void TransferCO2FromMCToIP() => TransferCO2FromMCToIPviaGR();


    #region Test functions

    /// <summary>
    /// General-purpose code tester. Put whatever you want here.
    /// </summary>
    protected override void Test()
    {
    }

    #endregion Test functions
}