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
        FTG_IMFlowManager = Find<FlowManager>("FTG_IMFlowManager");
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

        IM.Manometer.PropertyChanged += UpdateFlowRate;
        // Assume all of these sections share the same flow valve?
        IM_CT1.FlowValve.PropertyChanged -= UpdateFlowRate;
        //IM_CT1.FlowValve.PropertyChanged += UpdateFlowRate;
        //IM_CT2.FlowValve.PropertyChanged -= UpdateFlowRate;
        //IM_CT2.FlowValve.PropertyChanged += UpdateFlowRate;
        //IM_CA_CT1.FlowValve.PropertyChanged -= UpdateFlowRate;
        //IM_CA_CT1.FlowValve.PropertyChanged += UpdateFlowRate;
        //IM_CA_CT2.FlowValve.PropertyChanged -= UpdateFlowRate;
        //IM_CA_CT2.FlowValve.PropertyChanged += UpdateFlowRate;
    }
    #endregion HacsComponent

    #region System configuration
    #region HacsComponents
    IChamber ChamberCT1 { get; set; }
    public virtual HacsLog SampleRecords { get; set; }

    #region Sections

    /// <summary>
    /// The CT section, either CT1 or CT2, from which the CEGS should transfer the sample to the VTT.
    /// </summary>
    public override ISection CT
    {
        get => ct ?? CurrentCT;
        set => ct = value;
    }
    ISection ct;

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
    public ISection IP1 { get; set; }

    /// <summary>
    /// Flow-Through Gas..Inlet Port 1 section
    /// </summary>
    public ISection FTG_IP1 { get; set; }

    /// <summary>
    /// Flow-Through Gas..Inlet Manifold section
    /// </summary>
    public ISection FTG_IM { get; set; }

    /// <summary>
    /// Inlet Manifold..Coil Trap Flow section (bypasses CO2 analyzer)
    /// </summary>
    public ISection IM_CTF { get; set; }

    /// <summary>
    /// Inlet Manifold..CO2 Analyzer..Coil Trap Flow section
    /// </summary>
    public ISection IM_CA_CTF { get; set; }

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

    /// <summary>
    /// Coil Trap Flow..Coil Trap 1 section
    /// </summary>
    public ISection CTF_CT1 { get; set; }

    /// <summary>
    /// Coil Trap Flow..Coil Trap 2 section
    /// </summary>
    public ISection CTF_CT2 { get; set; }

    /// <summary>
    /// Coil Trap 1..Coil Trap Outlet section
    /// </summary>
    public ISection CT1_CTO { get; set; }

    /// <summary>
    /// Coil Trap 2..Coil Trap Outlet section
    /// </summary>
    public ISection CT2_CTO { get; set; }

    /// <summary>
    /// Coil Trap 1..Variable Temperature Trap section
    /// </summary>
    public ISection CT1_VTT { get; set; }

    /// <summary>
    /// Coil Trap 2..Variable Temperature Trap section
    /// </summary>
    public ISection CT2_VTT { get; set; }

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
    /// Flow manager for gas (He or O2) through Inlet Port 1 into the Inlet Manifold.
    /// </summary>
    public FlowManager FTG_IMFlowManager { get; set; }
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

        // Preparation for running samples
        Separators.Add(ProcessDictionary.Count);
        ProcessDictionary["Prepare GRs for new iron and desiccant"] = PrepareGRsForService;
        ProcessDictionary["Precondition GR iron"] = PreconditionGRs;
        ProcessDictionary["Replace iron in sulfur traps"] = ChangeSulfurFe;
        //ProcessDictionary["Service d13C ports"] = Service_d13CPorts;
        //ProcessDictionary["Load empty d13C ports"] = LoadEmpty_d13CPorts;
        //ProcessDictionary["Prepare loaded d13C ports"] = PrepareLoaded_d13CPorts;
        ProcessDictionary["Prepare loaded inlet ports for collection"] = PrepareIPsForCollection;
        
        // d13C ports prep
        Separators.Add(ProcessDictionary.Count);
        ProcessDictionary["Reload completed d13C ports"] = Reload_d13CPorts;
        ProcessDictionary["Prepare loaded d13C ports"] = PrepareLoaded_d13CPorts;
        
        // carbonate sample prep
        Separators.Add(ProcessDictionary.Count);
        ProcessDictionary["Prepare carbonate sample for acid"] = PrepareCarbonateSample;
        ProcessDictionary["Load acidified carbonate sample"] = LoadCarbonateSample;

        // Open line
        Separators.Add(ProcessDictionary.Count);
        ProcessDictionary["Open and evacuate line"] = OpenLine;
        ProcessDictionary["Open and evacuate VS1"] = () => OpenLine(Find<VacuumSystem>("VacuumSystem1"));
        ProcessDictionary["Open and evacuate VS2"] = () => OpenLine(Find<VacuumSystem>("VacuumSystem2"));

        // Main process continuations
        Separators.Add(ProcessDictionary.Count);
        ProcessDictionary["Collect, etc."] = CollectEtc;
        ProcessDictionary["Extract, etc."] = ExtractEtc;
        ProcessDictionary["Measure, etc."] = MeasureEtc;
        ProcessDictionary["Graphitize, etc."] = GraphitizeEtc;

        // Top-level steps for main process sequence
        Separators.Add(ProcessDictionary.Count);
        ProcessDictionary["Admit sealed CO2 to InletPort"] = AdmitSealedCO2IP;
        ProcessDictionary["Collect CO2 from InletPort"] = Collect;
        ProcessDictionary["Extract"] = Extract;
        ProcessDictionary["Measure"] = Measure;
        ProcessDictionary["Discard excess CO2 by splits"] = DiscardSplit;
        ProcessDictionary["Remove sulfur"] = RemoveSulfur;
        ProcessDictionary["Dilute small sample"] = Dilute;
        ProcessDictionary["Graphitize aliquots"] = GraphitizeAliquots;

        // Secondary-level process sub-steps
        Separators.Add(ProcessDictionary.Count);
        ProcessDictionary["Evacuate Inlet Port"] = EvacuateIP;
        ProcessDictionary["Flush Inlet Port"] = FlushIP;
        ProcessDictionary["Admit O2 into Inlet Port"] = AdmitIPO2;
        ProcessDictionary["Heat Quartz and Open Line"] = HeatQuartzOpenLine;
        ProcessDictionary["Turn off IP furnaces"] = TurnOffIPFurnaces;
        ProcessDictionary["Discard IP gases"] = DiscardIPGases;
        ProcessDictionary["Close IP"] = CloseIP;
        ProcessDictionary["Start collecting"] = StartCollecting;
        ProcessDictionary["Clear collection conditions"] = ClearCollectionConditions;
        ProcessDictionary["Collect until condition met"] = CollectUntilConditionMet;
        ProcessDictionary["Stop collecting"] = StopCollecting;
        ProcessDictionary["Stop collecting after bleed down"] = StopCollectingAfterBleedDown;
        ProcessDictionary["Evacuate and Freeze VTT"] = FreezeVtt;
        ProcessDictionary["Admit Dead CO2 into MC"] = AdmitDeadCO2;
        ProcessDictionary["Purify CO2 in MC"] = CleanupCO2InMC;
        ProcessDictionary["Discard MC gases"] = DiscardMCGases;
        ProcessDictionary["Divide sample into aliquots"] = DivideAliquots;

        // Granular inlet port & sample process control
        Separators.Add(ProcessDictionary.Count);
        ProcessDictionary["Turn on quartz furnace"] = TurnOnIpQuartzFurnace;
        ProcessDictionary["Turn off quartz furnace"] = TurnOffIpQuartzFurnace;
        ProcessDictionary["Disable sample setpoint ramping"] = DisableIpRamp;
        ProcessDictionary["Enable sample setpoint ramping"] = EnableIpRamp;
        ProcessDictionary["Turn on sample furnace"] = TurnOnIpSampleFurnace;
        ProcessDictionary["Adjust sample setpoint"] = AdjustIpSetpoint;
        ProcessDictionary["Adjust sample ramp rate"] = AdjustIpRampRate;
        ProcessDictionary["Wait for sample to rise to setpoint"] = WaitIpRiseToSetpoint;
        ProcessDictionary["Wait for sample to fall to setpoint"] = WaitIpFallToSetpoint;
        ProcessDictionary["Turn off sample furnace"] = TurnOffIpSampleFurnace;

        // General-purpose process control actions
        Separators.Add(ProcessDictionary.Count);
        ProcessDictionary["Wait for timer"] = WaitForTimer;
        ProcessDictionary["Wait for operator"] = WaitForOperator;
        ProcessDictionary["Wait for CEGS to be free"] = WaitForCegs;
        ProcessDictionary["Start Extract, Etc"] = StartExtractEtc;

        // Transferring CO2
        Separators.Add(ProcessDictionary.Count);
        ProcessDictionary["Transfer CO2 from CT to VTT"] = TransferCO2FromCTToVTT;
        ProcessDictionary["Transfer CO2 from MC to VTT"] = TransferCO2FromMCToVTT;
        ProcessDictionary["Transfer CO2 from MC to GR"] = TransferCO2FromMCToGR;
        ProcessDictionary["Transfer CO2 from prior GR to MC"] = TransferCO2FromGRToMC;

        // Flow control steps & special collection operations
        Separators.Add(ProcessDictionary.Count);
        ProcessDictionary["Reset tracked flow and collected µgc."] = ResetUgcTracking;
        ProcessDictionary["No IP flow"] = NoIpFlow;
        ProcessDictionary["Use IP flow"] = UseIpFlow;
        ProcessDictionary["Include CO2 analyzer"] = IncludeCO2Analyzer;
        ProcessDictionary["Bypass CO2 analyzer"] = BypassCO2Analyzer;
        ProcessDictionary["Select CT1"] = SelectCT1;
        ProcessDictionary["Select CT2"] = SelectCT2;
        ProcessDictionary["Start collecting"] = StartCollecting;
        ProcessDictionary["Clear collection conditions"] = ClearCollectionConditions;
        ProcessDictionary["Collect until condition met"] = CollectUntilConditionMet;
        ProcessDictionary["Toggle CT collection"] = ToggleCT;
        ProcessDictionary["Stop collecting"] = StopCollecting;
        ProcessDictionary["Stop collecting after bleed down"] = StopCollectingAfterBleedDown;

        // Flow control sub-steps
        Separators.Add(ProcessDictionary.Count);
        ProcessDictionary["Start flow through to trap"] = StartFlowThroughToTrap;
        ProcessDictionary["Start flow through to waste"] = StartFlowThroughToWaste;
        ProcessDictionary["Stop flow-through gas"] = StopFlowThroughGas;

        // d13C port service routines
        //Separators.Add(ProcessDictionary.Count);
        //ProcessDictionary["Empty completed d13C ports"] = EmptyCompleted_d13CPorts;
        //ProcessDictionary["Thaw frozen d13C ports"] = ThawFrozen_d13CPorts;
        //ProcessDictionary["Load empty d13C ports"] = LoadEmpty_d13CPorts;
        //ProcessDictionary["Prepare loaded d13C ports"] = PrepareLoaded_d13CPorts;


        // Utilities (generally not for sample processing)
        Separators.Add(ProcessDictionary.Count);
        ProcessDictionary["Exercise all Opened valves"] = ExerciseAllValves;
        ProcessDictionary["Close all Opened valves"] = CloseAllValves;
        ProcessDictionary["Exercise all LN Manifold valves"] = ExerciseLNValves;
        ProcessDictionary["Calibrate all multi-turn valves"] = CalibrateRS232Valves;
        ProcessDictionary["Measure MC volume (KV in MCP1)"] = MeasureVolumeMC;
        ProcessDictionary["Measure valve volumes (plug in MCP1)"] = MeasureValveVolumes;
        ProcessDictionary["Measure remaining chamber volumes"] = MeasureRemainingVolumes;
        ProcessDictionary["Check GR H2 density ratios"] = CalibrateGRH2;
        //ProcessDictionary["Calibrate VP N2 initial manifold pressure"] = CalibrateVPHeP0;
        ProcessDictionary["Measure Extraction efficiency"] = MeasureExtractEfficiency;
        ProcessDictionary["Measure IP collection efficiency"] = MeasureIpCollectionEfficiency;

        // Test functions
        Separators.Add(ProcessDictionary.Count);
        ProcessDictionary["Test"] = Test;
        base.BuildProcessDictionary();
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
        ProcessStep.Start("Close gas supplies");
        CloseGasSupplies();
        ProcessStep.End();

        var vacuumSystems = VacuumSystems.Values.ToList();

        // Which do we want? Do we want to be able to choose?
        vacuumSystems.ForEach(base.OpenLine);           // thaws coldfingers
        //vacuumSystems.ForEach(vs => vs.OpenLine());   // doesn't thaw coldfingers

        ProcessStep.Start($"Wait for all vacuum systems to reach {OkPressure} Torr");
        WaitFor(() => vacuumSystems.All(vs => vs.Pressure <= OkPressure));
        ProcessStep.End();

        ProcessStep.Start("Join vacuum system lines");
        // compute all pairs?
        Section.Connections(vacuumSystems.First().MySection, vacuumSystems.Last().MySection).Open();
        ProcessStep.End();

        ProcessStep.Start($"Isolate {CA.Name} (temp. due to leak)");
        CA.Isolate();
        ProcessStep.End();

    }

    #endregion OpenLine

    #region Process Control Parameters

    /// <summary>
    /// Stop collecting into the coil trap when amount of carbon in the Coil Trap reaches this value,
    /// provided that it is a number (i.e., not NaN).
    /// </summary>
    public double CollectUntilUgc => GetParameter("CollectUntilUgc");

    #endregion Process Control Parameters

    #region Process Control Properties

    /// <summary>
    /// Provide a flow of oxygen through the Inlet Port to combust the sample,
    /// instead of a fixed pressure.
    /// </summary>
    public virtual bool NeedIpFlow { get; set; } = false;

    /// <summary>
    /// Direct the sample gas through the CO2 analyzer during collection.
    /// </summary>
    public virtual bool NeedAnalyzer { get; set; } = false;

    /// <summary>
    /// The coil trap currently being used to trap sample gas.
    /// </summary>
    public ISection CurrentCT => base.IM_FirstTrap.Chambers.Contains(ChamberCT1) ? CT1 : CT2;

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
    /// Use a flow of oxygen through the Inlet Port to combust the sample.
    /// </summary>
    protected virtual void UseIpFlow() => NeedIpFlow = true;

    /// <summary>
    /// Provide a fixed amount (pressure) of oxygen into the Inlet Port
    /// to combust the sample.
    /// </summary>
    protected virtual void NoIpFlow() => NeedIpFlow = false;

    /// <summary>
    /// Start flowing O2 through the Inlet Port and the (warm) coil trap to vacuum.
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
        ProcessStep.Start($"Start flowing O2 through {InletPort.Name}");

        var source = FTG_IP1;
        var gasfm = FTG_IMFlowManager;
        // Need to manage the upstream FTG gas supply valve manually, because we want the shutoff to be
        // downstream of the flow valve.
        var o2 = Find<IValve>("vO2_FTG");

        ProcessStep.Start($"Isolate and open section {source.Name}.");
        source.Isolate();
        source.Open();
        ProcessStep.End();

        gasfm.FlowValve.CloseWait();

        IM_FirstTrap.FlowManager.StopOnFullyOpened = false;
        StartSampleFlow(trap);          // Manage CT flow to maintain bleed pressure
        o2.OpenWait();

        // TODO magic number "IMFlowPressure"? (make parameter)
        gasfm.Start(20);                // Manage supply flow to maintain IM pressure

        ProcessStep.End();
    }

    /// <summary>
    /// Stop flowing O2 into the Inlet Port.
    /// </summary>
    protected virtual void StopFlowThroughGas()
    {
        ProcessStep.Start($"Stopping O2 flow into {InletPort.Name}");

        var gasfm = FTG_IMFlowManager;
        var supplyValve = Find<IValve>("vO2_FTG");
        var gasSupply = GasSupply("O2", IM);

        gasfm.Stop();
        gasSupply.ShutOff();
        supplyValve.CloseWait();
        gasfm.FlowValve.CloseWait();

        ProcessStep.End();
    }

    /// <summary>
    /// Include the CO2 Analyzer in the sample gas collection path.
    /// </summary>
    protected virtual void IncludeCO2Analyzer() => NeedAnalyzer = true;

    /// <summary>
    /// Bypass the CO2 Analyzer when collecting sample gas.
    /// </summary>
    protected virtual void BypassCO2Analyzer() => NeedAnalyzer = false;

    /// <summary>
    /// Use Coil Trap 1 for sample collection;
    /// </summary>
    protected virtual void SelectCT1() => base.IM_FirstTrap = NeedAnalyzer ? IM_CA_CT1 : IM_CT1;

    /// <summary>
    /// Use Coil Trap 2 for sample collection.
    /// </summary>
    protected virtual void SelectCT2() => base.IM_FirstTrap = NeedAnalyzer ? IM_CA_CT2 : IM_CT2;

    /// <summary>
    /// Switch coil traps.
    /// </summary>
    protected virtual void ToggleCT()
    {
        ProcessStep.Start($"Toggle CT");

        if (CT == CT1)
            SelectCT2();
        else
            SelectCT1();
        StartCollecting();

        ProcessStep.End();
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
    /// Set all collection condition parameters to NaN
    /// </summary>
    protected override void ClearCollectionConditions()
    {
        base.ClearCollectionConditions();
        ClearParameter("CollectUntilUgc");
    }


    /// <summary>
    /// Wait for a collection stop condition to occur.
    /// </summary>
    protected override void CollectUntilConditionMet()
    {
        ProcessStep.Start($"Wait for a collection stop condition");
        IpIm(out ISection im);

        var maximumSampleTemperatureStopwatch = new Stopwatch();
        var maximumSampleTemperature = GetParameter("MaximumSampleTemperature");
        var minutesAtMaximumTemperature = GetParameter("MinutesAtMaximumTemperature");

        string stoppedBecause = "";
        bool shouldStop()
        {
            if (CollectStopwatch.IsRunning && CollectStopwatch.ElapsedMilliseconds < 1000)
                return false;

            // TODO: what if flow manager becomes !Busy (because, e.g., FlowValve is fully open)?
            // TODO: should we invoke DuringBleed()? When?
            // TODO: should we disable/enable CT.VacuumSystem.Manometer?

            // Open flow bypass when conditions allow it without producing an excessive
            // downstream pressure spike.
            if (im != null && im.Pressure - FirstTrap.Pressure < FirstTrapFlowBypassPressure)
                FirstTrap.Open();   // open bypass if available
                                    // (REVISIT THIS, normally we don't want Open to
                                    // open the flow valve or bypass....


            if (CollectCloseIpAtPressure.IsANumber() && InletPort.IsOpened && im != null && im.Pressure <= CollectCloseIpAtPressure)
            {
                var p = im.Pressure;
                InletPort.Close();
                SampleLog.Record($"{Sample.LabId}\tClosed {InletPort.Name} at {im.Manometer.Name} = {p:0} Torr");
            }
            if (CollectCloseIpAtCtPressure.IsANumber() && InletPort.IsOpened && FirstTrap.Pressure <= CollectCloseIpAtCtPressure)
            {
                var p = FirstTrap.Pressure;
                InletPort.Close();
                SampleLog.Record($"{Sample.LabId}\tClosed {InletPort.Name} at {FirstTrap.Manometer.Name} = {p:0} Torr");
            }

            if (Stopping)
            {
                stoppedBecause = "CEGS is shutting down";
                return true;
            }

            if (maximumSampleTemperature.IsANumber() && minutesAtMaximumTemperature.IsANumber())
            {
                if (maximumSampleTemperatureStopwatch.IsRunning)
                {
                    if (maximumSampleTemperatureStopwatch.Elapsed.TotalMinutes > minutesAtMaximumTemperature)
                    {
                        stoppedBecause = $"Reached {minutesAtMaximumTemperature} minutes at maximum temperature ({maximumSampleTemperature} °C)";
                        return true;
                    }
                }
                else if (InletPort.Temperature >= maximumSampleTemperature)
                    maximumSampleTemperatureStopwatch.Restart();
            }

            if (CollectUntilTemperatureRises.IsANumber() && InletPort.Temperature >= CollectUntilTemperatureRises)
            {
                stoppedBecause = $"InletPort.Temperature rose to {CollectUntilTemperatureRises:0} °C";
                return true;
            }
            if (CollectUntilTemperatureFalls.IsANumber() && InletPort.Temperature <= CollectUntilTemperatureFalls)
            {
                stoppedBecause = $"InletPort.Temperature fell to {CollectUntilTemperatureFalls:0} °C";
                return true;
            }

            if (CollectUntilCtPressureFalls.IsANumber() &&
                FirstTrap.Pressure <= CollectUntilCtPressureFalls &&
                (im == null || im.Pressure < Math.Ceiling(CollectUntilCtPressureFalls) + 2))
            {
                stoppedBecause = $"{FirstTrap.Name}.Pressure fell to {CollectUntilCtPressureFalls:0.00} Torr";
                return true;
            }

            // old?: FirstTrap.Pressure < FirstTrapEndPressure;
            if (FirstTrapEndPressure.IsANumber() &&
                FirstTrap.Pressure <= FirstTrapEndPressure &&
                (im == null || im.Pressure < Math.Ceiling(FirstTrapEndPressure) + 2))
            {
                stoppedBecause = $"{FirstTrap.Name}.Pressure fell to {FirstTrapEndPressure:0.00} Torr";
                return true;
            }
            if (CollectUntilUgc.IsANumber() && CollectedUgc >= CollectUntilUgc)
            {
                stoppedBecause = $"Collected {CollectUntilUgc:0} µg C";
                return true;
            }
            if (CollectUntilMinutes.IsANumber() && CollectStopwatch.Elapsed.TotalMinutes >= CollectUntilMinutes)
            {
                stoppedBecause = $"{MinutesString((int)CollectUntilMinutes)} elapsed";
                return true;
            }

            stoppedBecause = "";
            return false;
        }
        WaitFor(shouldStop, -1, 1000);
        SampleLog.Record($"{Sample.LabId}\tStopped collecting:\t{stoppedBecause}");

        ProcessStep.End();
    }


    // TODO take a look at trying to use or extend the base version.
    /// <summary>
    /// Stop collecting. If 'immediately' is false, wait for CT pressure to bleed down after closing IP
    /// </summary>
    /// <param name="immediately">If false, wait for CT pressure to bleed down after closing IP</param>
    protected override void StopCollecting(bool immediately = true)
    {
        ProcessStep.Start("Stop Collecting");

        CT = CurrentCT;     // The VTT will take it from here
        IM_FirstTrap.FlowManager?.Stop();
        InletPort.Close();
        if (!immediately)
            FinishCollecting();
        IM_FirstTrap.Close();
        CT.Isolate();
        IM_FirstTrap.FlowValve.CloseWait();

        ProcessStep.End();
    }


    /// <summary>
    /// Wait for the CEGS to be ready to process a sample.
    /// ("CEGS" in this case is the section from the VTT onward.)
    /// </summary>
    protected virtual void WaitForCegs()
    {
        ProcessStep.Start("Wait for CEGS to be ready to process sample");

        bool cegsAvailable()
        {
            if (Stopping)
                return true;
            return CegsTask?.IsCompleted ?? true;
        }
        WaitFor(cegsAvailable, -1, 1000);

        ProcessStep.End();
    }

    /// <summary>
    /// Keep LN manifolds active for faster processing.
    /// </summary>
    protected override void ExtractEtc()
    {
        var lnManifolds = FindAll<LNManifold>();
        lnManifolds.ForEach(m => m.StayActive());
        base.ExtractEtc();
        lnManifolds.ForEach(m => m.Monitor());
    }

    /// <summary>
    /// Initiate the ExtractEtc process step, followed by evacuating VS2,
    /// to run concurrently while the Collection process continues.
    /// </summary>
    protected virtual void StartExtractEtc()
    {
        CegsTask = Task.Run(ExtractEtcThenEvacuate);
    }

    /// <summary>
    /// Run the ExtractEtc process step, then evacuate the line.
    /// </summary>
    protected virtual void ExtractEtcThenEvacuate()
    {
        ProcessStep.Start($"Extract from {CT.Name}");

        TransferCO2FromCTToVTT();
        ExtractEtc();
        var vacuumSystem = GM.VacuumSystem;
        OpenLine(vacuumSystem);          // TODO: decide between this and vacuumSystem.OpenLine()
        vacuumSystem.WaitForPressure(OkPressure);

        ProcessStep.End();
    }


    /// <summary>
    /// To torch them off
    /// </summary>
    protected void FreezeCompleted_d13CPorts()
    {
        var ports = d13CPorts.FindAll(p => p.State == LinePort.States.Complete);
        ProcessStep.Start("Freeze completed d13C ports");
        ports.ForEach(p => p.Coldfinger.Freeze());
        WaitFor(() => ports.All(p => p.Coldfinger.Frozen));
        ProcessStep.End();
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
    protected override void SampleRecord(IAliquot aliquot)
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

    protected void RampedOxidation()
    {
        SetParameter("HoldSampleAtPorts", 1);

        FTG_IP1.Close();
        IM.VacuumSystem.OpenLine(); // don't thaw coldfingers
        EvacuateIP(OkPressure);
        FlushIP();        

        IM.ClosePortsExcept(InletPort);
        while (PortLeakRate(InletPort) > LeakTightTorrLitersPerSecond)
        {
            if (Warn($"{InletPort.Name} is leaking.",
                "Process Paused.\r\n" +
                $"Ok to try again or Cancel to move on.\r\n" +
                $"Restart the application to abort the process.").Cancelled())
                break;
        }

        //TODO change this to CleanPressure? It's OkPressure for process testing.
        EvacuateIP(OkPressure);

        ProcessStep.Start($"Heat Quartz");
        TurnOnIpQuartzFurnace();
        WaitMinutes((int)QuartzFurnaceWarmupMinutes);
        ProcessStep.End();

        IncludeCO2Analyzer();
        //BypassCO2Analyzer();		 // for testing?
        SelectCT1();
        UseIpFlow();
        StartFlowThroughToTrap();
        ResetUgcTracking();

        SetParameter("IpSetpoint", 1000);
        SetParameter("IpRampRate", 5);
        EnableIpRamp();
        TurnOnIpSampleFurnace();

        List<Sample> Splits = new() { Sample as Sample };
        var numberOfSplits = GetParameter("NumberOfSplits");
        if (!numberOfSplits.IsANumber())
            numberOfSplits = 1;

        for (int i = 1; i <= numberOfSplits; i++)
        {
            if (i > 1)
            {
                // Clone the sample for subsequent splits
                Sample = Sample.Clone();
                Sample.DateTime = DateTime.Now;
                Sample.MicrogramsDilutionCarbon = 0;
                Sample.TotalMicrogramsCarbon = 0;
                Sample.Discards = 0;
                Sample.SelectedMicrogramsCarbon = 0;
                Sample.Micrograms_d13C = 0;
                Sample.d13CPartsPerMillion = 0;
                // Use autogeneratedgraphite number for Aliquot IDs?
                // If not, we need a scheme to make them unique.
                // Prefix with Sample.Name?

                Splits.Add(Sample as Sample);
                ToggleCT();
            }

            ClearCollectionConditions();

            var targetUgc = GetValidParameter($"Split{i}TargetUgc");
            if (!targetUgc.IsANumber()) return;
            SetParameter("CollectUntilUgc", targetUgc);
            SetParameter("MaximumSampleTemperature", 1000);
            SetParameter("MinutesAtMaximumTemperature", 10);

            CollectUntilConditionMet();

            WaitForCegs();
            StartExtractEtc();
        }

        Splits.ForEach(s =>
        {
            Sample = s;

            // Re-freeze and start graphite reactor(s) (typically only 1 per split).
            Sample.Aliquots.ForEach(a =>
                Find<GraphiteReactor>(a.GraphiteReactor).Coldfinger.Raise());
            GraphitizeAliquots();

            AddCarrierTo_d13C();
        });

        OpenLine();
    }

    #endregion Process Steps

    #endregion Process Management

    #region Test functions

    protected virtual void FtgPressurize(string gasName, ISection destination, double pressure)
    {
        // Need to manage the upstream FTG gas supply valve manually, because we want the shutoff to be
        // downstream of the flow valve.
        var vGas = Find<IValve>($"v{gasName}_FTG");
        var gs = GasSupply(gasName, destination);
        vGas.OpenWait();
        gs.Pressurize(pressure);
        vGas.CloseWait();
    }

    #region Coil trap flow rate testing and estimation

    /// <summary>
    /// Coil trap flow rate in standard cubic centimeters per minute.
    /// Rough draft / prototype version.
    /// See "CT flow analysis.ods".
    /// </summary>
    /// <returns></returns>
    public double CoilTrapSccm
    {
        get
        {
            if (!IM_CT1.IsOpened &&
                !IM_CT2.IsOpened &&
                !IM_CA_CT1.IsOpened &&
                !IM_CA_CT2.IsOpened)
                return 0;
            var pUp = ImPressure;      // upstream pressure
            if (pUp < 0.3) pUp = 0;    // treat very low upstream pressure as 0.
            var pos = CtFlowValvePosition;

            // pIM => sccm model
            // The flow rate model is a polynomial of degree 2.
            // The reference model was obtained by testing the flow rate
            // with flow valve set to Position 667.
            // Tests were then performed for several other valve positions
            // and the data was analyzed to produce second-degree polynomials for
            // each position.
            // The coefficients of all of these polynomials were plotted against the
            // valve position, and trend lines were constructed to fit the curves.
            // The trend line equations are used here to calculate 2nd-degree
            // coefficients from the relative valve position. 
            //var refPos = 667;                   // model reference position
            //var dpos = refPos - pos;            // relative valve position
            var c0 = 0;         // x^0 coefficient; this is indistinguishable from zero 
            var c1 = 0.34038 + pos * (3.4682e-4 + pos * (-1.7704e-6 + pos * 7.1666e-10));  // x^1 coefficient
            if (c1 < 0.002151) c1 = 0.002151;     // Pos 667
            var c2 = 0.033837 - 5.2801e-5 * pos;   // x^2
            if (c2 < 1.0901e-5) c2 = 1.0901e-5;     // Pos 667
            var sscm = c0 + pUp * (c1 + pUp * c2);   // evaluate the polynomial
            return sscm;
        }
    }

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


    double ImPressure;
    int CtFlowValvePosition;
    protected virtual void UpdateFlowRate(object sender, PropertyChangedEventArgs e)
    {
        var property = e.PropertyName;
        var update = false;
        if (sender == IM.Manometer && property == nameof(IM.Manometer.Value) && ImPressure != IM.Manometer.Value)
        {
            ImPressure = Math.Max(0, IM.Manometer.Value);
            update = true;
        }
        if (CtFlowValvePosition != IM_FirstTrap.FlowValve.Position)
        {
            CtFlowValvePosition = IM_FirstTrap.FlowValve.Position;
            update = true;
        }

        if (update)
            CtFlowMonitor.FlowRateMeter.Update(CoilTrapSccm);
    }


    protected virtual (double, double) CTFlowTest(double pressure, int position)
    {
        var dest = Find<Section>("IM_CA");
        var vFlow = IM_CA_CT1.FlowManager.FlowValve;

        ProcessStep.Start("Evacuate IM_CA_CT1");
        IM_CA_CT1.Isolate();
        vFlow.Open();
        IM_CA_CT1.OpenAndEvacuate(0.005);
        vFlow.Close();
        ProcessStep.End();

        FtgPressurize("O2", dest, pressure);

        ProcessStep.Start("Bleed IM_CA down until pCT < 0.5 Torr");
        IM_CA_CT1.OpenAndEvacuate();
        vFlow.MoveTo(position);
        WaitSeconds(5);
        var p0 = IM.Pressure;
        WaitFor(() => CTF.Pressure < 0.5);
        var p1 = IM.Pressure;
        ProcessStep.End();

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

    #endregion Coil trap flow rate testing and model

    protected override void TransferCO2FromMCToIP() => TransferCO2FromMCToIPviaGR();

    /// <summary>
    /// General-purpose code tester. Put whatever you want here.
    /// </summary>
    protected override void Test()
    {
        ResetUgcTracking();
    }

    #endregion Test functions

}