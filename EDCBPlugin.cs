using InfoPanel.Plugins;
using System.Data;
using System.Xml.Linq;

namespace EDCBMonitorPlugin;

public class EDCBPlugin : BasePlugin
{
    private readonly EDCBApiClient _api = new();

    public override string? ConfigFilePath => null;
    public override TimeSpan UpdateInterval => TimeSpan.FromSeconds(30);

    //
    // Reservation
    //
    private readonly PluginSensor _reserveCount =
        new("Reserve Count", 0, "");

    private readonly PluginSensor _todayReserveCount =
        new("Today Reserve Count", 0, "");

    private readonly PluginSensor _minutesUntilNext =
        new("Minutes Until Next Recording", 0, "min");

    //
    // Recording
    //
    private readonly PluginSensor _recordedCount =
        new("Recorded Count", 0, "");

    private readonly PluginSensor _failedRecordingCount =
        new("Failed Recording Count", 0, "");

    private readonly PluginSensor _recordingFlag =
        new("Recording Flag", 0, "");

    private readonly PluginSensor _recordingCount =
        new("Recording Count", 0, "");

    private readonly PluginSensor _recordingRemainingMinutes =
        new("Recording Remaining Minutes", 0, "min");

    //
    // Tuner
    //
    private readonly PluginSensor _tunerCount =
        new("Tuner Count", 0, "");

    private readonly PluginSensor _tunerUsedCount =
        new("Tuner Used Count", 0, "");

    private readonly PluginSensor _freeTunerCount =
        new("Free Tuner Count", 0, "");

    private readonly PluginSensor _tunerUsage =
        new("Tuner Usage", 0, "%");

    //
    // Text
    //
    private readonly PluginText _nextReservation =
        new("Next Reservation", "-");

    private readonly PluginText _nextReservation2 =
        new("Next Reservation 2", "-");

    private readonly PluginText _nextReservation3 =
        new("Next Reservation 3", "-");

    private readonly PluginText _nextChannel =
        new("Next Reservation Channel", "-");

    private readonly PluginText _nextChannel2 =
        new("Next Reservation 2 Channel", "-");

    private readonly PluginText _nextStart =
        new("Next Start", "-");

    private readonly PluginText _lastRecorded =
        new("Last Recorded", "-");

    private readonly PluginText _currentRecording =
        new("Current Recording", "-");

    private readonly PluginText _recordingTitle2 =
        new("Recording Title 2", "-");

    private readonly PluginText _recordingTitle3 =
        new("Recording Title 3", "-");

    private readonly PluginText _recordingChannel =
        new("Recording Channel", "-");

    private readonly PluginText _recordingEndTime =
        new("Recording End Time", "-");

    //
    // Tables
    //
    private readonly PluginTable _upcomingReservations =
        new("Upcoming Reservations",
            new DataTable(),
            "0:400|1:180|2:140|3:80");

    private readonly PluginTable _recordedPrograms =
        new("Recorded Programs",
            new DataTable(),
            "0:400|1:180|2:80|3:80");

    public EDCBPlugin()
        : base(
            "edcb-monitor",
            "EDCB Monitor",
            "EDCB reservation and recording monitor")
    {
    }

    public override void Initialize()
    {
    }

    public override void Load(List<IPluginContainer> containers)
    {
        PluginContainer container = new("EDCB");

        //
        // Reservation
        //
        container.Entries.Add(_reserveCount);
        container.Entries.Add(_todayReserveCount);
        container.Entries.Add(_minutesUntilNext);

        //
        // Recording
        //
        container.Entries.Add(_recordedCount);
        container.Entries.Add(_failedRecordingCount);
        container.Entries.Add(_recordingFlag);
        container.Entries.Add(_recordingCount);
        container.Entries.Add(_recordingRemainingMinutes);

        //
        // Tuner
        //
        container.Entries.Add(_tunerCount);
        container.Entries.Add(_tunerUsedCount);
        container.Entries.Add(_freeTunerCount);
        container.Entries.Add(_tunerUsage);

        //
        // Next Reservations
        //
        container.Entries.Add(_nextReservation);
        container.Entries.Add(_nextReservation2);
        container.Entries.Add(_nextReservation3);

        container.Entries.Add(_nextChannel);
        container.Entries.Add(_nextChannel2);

        container.Entries.Add(_nextStart);

        //
        // Recording Now
        //
        container.Entries.Add(_currentRecording);
        container.Entries.Add(_recordingTitle2);
        container.Entries.Add(_recordingTitle3);

        container.Entries.Add(_recordingChannel);
        container.Entries.Add(_recordingEndTime);

        //
        // Last Recording
        //
        container.Entries.Add(_lastRecorded);

        //
        // Tables
        //
        container.Entries.Add(_upcomingReservations);
        container.Entries.Add(_recordedPrograms);

        containers.Add(container);
    }

    public override void Update()
    {
    }
public override async Task UpdateAsync(CancellationToken cancellationToken)
{
    try
    {
        //
        // Reserve Info
        //
        var reserveDoc = await _api.GetReserveInfoAsync();

        var reserves =
            reserveDoc.Descendants("reserveinfo")
                      .ToList();

        _reserveCount.Value = reserves.Count;

        _todayReserveCount.Value =
            reserves.Count(r =>
            {
                return r.Element("startDate")?.Value ==
                    DateTime.Today.ToString("yyyy/MM/dd");
            });

        var now = DateTime.Now;

        var reserveList =
            reserves
            .Select(r =>
            {
                DateTime.TryParse(
                    $"{r.Element("startDate")?.Value} {r.Element("startTime")?.Value}",
                    out var start);

                int.TryParse(
                    r.Element("duration")?.Value,
                    out int duration);

                return new
                {
                    Title =
                        r.Element("title")?.Value ?? "-",

                    Channel =
                        r.Element("service_name")?.Value ?? "-",

                    Start = start,

                    Duration = duration,

                    End =
                        start.AddSeconds(duration)
                };
            })
            .OrderBy(x => x.Start)
            .ToList();

        //
        // Next Reservations
        //
        var nextReserve =
            reserveList
            .Where(x => x.Start > now)
            .ToList();

        if (nextReserve.Count > 0)
        {
            _nextReservation.Value =
                nextReserve[0].Title;

            _nextChannel.Value =
                nextReserve[0].Channel;

            _nextStart.Value =
                nextReserve[0]
                    .Start
                    .ToString("MM/dd HH:mm");

            _minutesUntilNext.Value =
                (float)
                (nextReserve[0].Start - now)
                .TotalMinutes;
        }
        else
        {
            _nextReservation.Value = "-";
            _nextChannel.Value = "-";
            _nextStart.Value = "-";
            _minutesUntilNext.Value = 0;
        }

        if (nextReserve.Count > 1)
        {
            _nextReservation2.Value =
                nextReserve[1].Title;

            _nextChannel2.Value =
                nextReserve[1].Channel;
        }
        else
        {
            _nextReservation2.Value = "-";
            _nextChannel2.Value = "-";
        }

        if (nextReserve.Count > 2)
        {
            _nextReservation3.Value =
                nextReserve[2].Title;
        }
        else
        {
            _nextReservation3.Value = "-";
        }

        //
        // Recording Now
        //
        var recordingNow =
            reserveList
            .Where(x =>
                now >= x.Start &&
                now <= x.End)
            .ToList();

        _recordingFlag.Value =
            recordingNow.Count > 0 ? 1 : 0;

        _recordingCount.Value =
            recordingNow.Count;

        if (recordingNow.Count > 0)
        {
            _currentRecording.Value =
                recordingNow[0].Title;

            _recordingChannel.Value =
                recordingNow[0].Channel;

            _recordingEndTime.Value =
                recordingNow[0]
                .End
                .ToString("HH:mm");

            _recordingRemainingMinutes.Value =
                (float)
                (recordingNow[0].End - now)
                .TotalMinutes;
        }
        else
        {
            _currentRecording.Value = "-";
            _recordingChannel.Value = "-";
            _recordingEndTime.Value = "-";
            _recordingRemainingMinutes.Value = 0;
        }

        if (recordingNow.Count > 1)
        {
            _recordingTitle2.Value =
                recordingNow[1].Title;
        }
        else
        {
            _recordingTitle2.Value = "-";
        }

        if (recordingNow.Count > 2)
        {
            _recordingTitle3.Value =
                recordingNow[2].Title;
        }
        else
        {
            _recordingTitle3.Value = "-";
        }
        //
        // Upcoming Reservations Table
        //
        DataTable reserveTable = new();

        reserveTable.Columns.Add("Title");
        reserveTable.Columns.Add("Channel");
        reserveTable.Columns.Add("Start");
        reserveTable.Columns.Add("Duration");

        foreach (var item in nextReserve.Take(50))
        {
            reserveTable.Rows.Add(
                item.Title,
                item.Channel,
                item.Start.ToString("MM/dd HH:mm"),
                TimeSpan
                    .FromSeconds(item.Duration)
                    .ToString(@"hh\:mm"));
        }

        _upcomingReservations.Value =
            reserveTable;

        //
        // Tuner Info
        //
        var tunerDoc =
            await _api.GetTunerInfoAsync();

        var tuners =
            tunerDoc
                .Descendants("tuner")
                .ToList();

        _tunerCount.Value =
            tuners.Count;

        int used =
            tuners.Count(t =>
                t.Descendants("reserveinfo")
                 .Any());

        _tunerUsedCount.Value =
            used;

        _freeTunerCount.Value =
            Math.Max(
                0,
                tuners.Count - used);

        _tunerUsage.Value =
            tuners.Count == 0
                ? 0
                : used * 100f /
                  tuners.Count;

        //
        // Recorded Info
        //
        var recDoc =
            await _api.GetRecInfoAsync();

        var recs =
            recDoc
                .Descendants("recinfo")
                .ToList();

        _recordedCount.Value =
            recs.Count;

        _failedRecordingCount.Value =
            recs.Count(r =>
            {
                int.TryParse(
                    r.Element("drops")?.Value,
                    out int drops);

                int.TryParse(
                    r.Element("scrambles")?.Value,
                    out int scrambles);

                return drops > 0 ||
                       scrambles > 0;
            });

        if (recs.Count > 0)
        {
            _lastRecorded.Value =
                recs[0]
                .Element("title")
                ?.Value ?? "-";
        }
        else
        {
            _lastRecorded.Value = "-";
        }

        //
        // Recorded Programs Table
        //
        DataTable recTable = new();

        recTable.Columns.Add("Title");
        recTable.Columns.Add("Channel");
        recTable.Columns.Add("Drops");
        recTable.Columns.Add("Scrambles");

        foreach (var rec in recs.Take(100))
        {
            recTable.Rows.Add(
                rec.Element("title")?.Value ?? "",
                rec.Element("service_name")?.Value ?? "",
                rec.Element("drops")?.Value ?? "0",
                rec.Element("scrambles")?.Value ?? "0");
        }

        _recordedPrograms.Value =
            recTable;
    }
    catch
    {
        //
        // 通信エラー時は前回値を保持
        //
    }
}

public override void Close()
{
}

}

