using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Net.NetworkInformation;
using System.Net.Sockets;
using System.Text.RegularExpressions;
using System.Windows.Threading;
using System.Windows.Media;
using LibreHardwareMonitor.Hardware;
using Newtonsoft.Json;
using SidebarDiagnostics.Framework;

namespace SidebarDiagnostics.Monitoring
{
    public class MonitorManager : INotifyPropertyChanged, IDisposable
    {
        public MonitorManager(MonitorConfig[] config)
        {
            _computer = new Computer()
            {
                IsCpuEnabled = true,
                IsControllerEnabled = true,
                IsGpuEnabled = true,
                IsStorageEnabled = false,
                IsMotherboardEnabled = true,
                IsMemoryEnabled = true,
                IsNetworkEnabled = false
            };
            _computer.Open();
            _board = GetHardware(HardwareType.Motherboard).FirstOrDefault();

            UpdateBoard();

            MonitorPanels = config.Where(c => c.Enabled).OrderByDescending(c => c.Order).Select(c => NewPanel(c)).ToArray();
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (!_disposed)
            {
                if (disposing)
                {
                    foreach (MonitorPanel _panel in MonitorPanels)
                    {
                        _panel.Dispose();
                    }

                    _computer.Close();

                    _monitorPanels = null;
                    _computer = null;
                    _board = null;
                }

                _disposed = true;
            }
        }

        ~MonitorManager()
        {
            Dispose(false);
        }

        public HardwareConfig[] GetHardware(MonitorType type)
        {
            switch (type)
            {
                case MonitorType.CPU:
                case MonitorType.RAM:
                case MonitorType.GPU:
                    return GetHardware(type.GetHardwareTypes()).Select(h => new HardwareConfig() { ID = h.Identifier.ToString(), Name = h.Name, ActualName = h.Name }).ToArray();

                case MonitorType.HD:
                    return DriveMonitor.GetHardware().ToArray();

                case MonitorType.Network:
                    return NetworkMonitor.GetHardware().ToArray();

                default:
                    throw new ArgumentException("Invalid MonitorType.");
            }
        }

        public void Update()
        {
            UpdateBoard();

            foreach (iMonitor _monitor in MonitorPanels.SelectMany(p => p.Monitors))
            {
                _monitor.Update();
            }
        }

        public void NotifyPropertyChanged(string propertyName)
        {
            if (PropertyChanged != null)
            {
                PropertyChanged(this, new PropertyChangedEventArgs(propertyName));
            }
        }

        public event PropertyChangedEventHandler PropertyChanged;

        private IEnumerable<IHardware> GetHardware(params HardwareType[] types)
        {
            return _computer.Hardware.Where(h => types.Contains(h.HardwareType));
        }

        private MonitorPanel NewPanel(MonitorConfig config)
        {
            switch (config.Type)
            {
                case MonitorType.CPU:
                    return OHMPanel(
                        config.Type,
                        "M 19,19L 57,19L 57,22.063C 56.1374,22.285 55.5,23.0681 55.5,24C 55.5,24.9319 56.1374,25.715 57,25.937L 57,57L 19,57L 19,27.937C 19.8626,27.715 20.5,26.9319 20.5,26C 20.5,25.0681 19.8626,24.285 19,24.063L 19,19 Z M 21.9998,22.0005L 21.9998,24.0005L 23.9998,24.0005L 23.9998,22.0005L 21.9998,22.0005 Z M 24.9998,22.0005L 24.9998,24.0005L 26.9998,24.0005L 26.9998,22.0005L 24.9998,22.0005 Z M 27.9998,22.0005L 27.9998,24.0005L 29.9998,24.0005L 29.9998,22.0005L 27.9998,22.0005 Z M 30.9998,22.0005L 30.9998,24.0005L 32.9998,24.0005L 32.9998,22.0005L 30.9998,22.0005 Z M 33.9998,22.0005L 33.9998,24.0005L 35.9998,24.0005L 35.9998,22.0005L 33.9998,22.0005 Z M 36.9998,22.0005L 36.9998,24.0005L 38.9998,24.0005L 38.9998,22.0005L 36.9998,22.0005 Z M 39.9998,22.0005L 39.9998,24.0005L 41.9998,24.0005L 41.9998,22.0005L 39.9998,22.0005 Z M 42.9995,22.0005L 42.9995,24.0005L 44.9995,24.0005L 44.9995,22.0005L 42.9995,22.0005 Z M 45.9995,22.0005L 45.9995,24.0005L 47.9995,24.0005L 47.9995,22.0005L 45.9995,22.0005 Z M 48.9995,22.0004L 48.9995,24.0004L 50.9995,24.0004L 50.9995,22.0004L 48.9995,22.0004 Z M 51.9996,22.0004L 51.9996,24.0004L 53.9996,24.0004L 53.9996,22.0004L 51.9996,22.0004 Z M 21.9998,25.0004L 21.9998,27.0004L 23.9998,27.0004L 23.9998,25.0004L 21.9998,25.0004 Z M 24.9998,25.0004L 24.9998,27.0004L 26.9998,27.0004L 26.9998,25.0004L 24.9998,25.0004 Z M 27.9998,25.0004L 27.9998,27.0004L 29.9998,27.0004L 29.9998,25.0004L 27.9998,25.0004 Z M 30.9998,25.0004L 30.9998,27.0004L 32.9998,27.0004L 32.9998,25.0004L 30.9998,25.0004 Z M 33.9998,25.0004L 33.9998,27.0004L 35.9998,27.0004L 35.9998,25.0004L 33.9998,25.0004 Z M 36.9998,25.0004L 36.9998,27.0004L 38.9998,27.0004L 38.9998,25.0004L 36.9998,25.0004 Z M 39.9998,25.0004L 39.9998,27.0004L 41.9998,27.0004L 41.9998,25.0004L 39.9998,25.0004 Z M 42.9996,25.0004L 42.9996,27.0004L 44.9996,27.0004L 44.9996,25.0004L 42.9996,25.0004 Z M 45.9996,25.0004L 45.9996,27.0004L 47.9996,27.0004L 47.9996,25.0004L 45.9996,25.0004 Z M 48.9996,25.0004L 48.9996,27.0004L 50.9996,27.0004L 50.9996,25.0004L 48.9996,25.0004 Z M 51.9996,25.0004L 51.9996,27.0004L 53.9996,27.0004L 53.9996,25.0004L 51.9996,25.0004 Z M 21.9998,28.0004L 21.9998,30.0004L 23.9998,30.0004L 23.9998,28.0004L 21.9998,28.0004 Z M 24.9998,28.0004L 24.9998,30.0004L 26.9998,30.0004L 26.9998,28.0004L 24.9998,28.0004 Z M 27.9998,28.0004L 27.9998,30.0004L 29.9998,30.0004L 29.9998,28.0004L 27.9998,28.0004 Z M 30.9998,28.0004L 30.9998,30.0004L 32.9998,30.0004L 32.9998,28.0004L 30.9998,28.0004 Z M 33.9998,28.0004L 33.9998,30.0004L 35.9998,30.0004L 35.9998,28.0004L 33.9998,28.0004 Z M 36.9998,28.0004L 36.9998,30.0004L 38.9998,30.0004L 38.9998,28.0004L 36.9998,28.0004 Z M 39.9998,28.0004L 39.9998,30.0004L 41.9998,30.0004L 41.9998,28.0004L 39.9998,28.0004 Z M 42.9996,28.0004L 42.9996,30.0004L 44.9996,30.0004L 44.9996,28.0004L 42.9996,28.0004 Z M 45.9997,28.0004L 45.9997,30.0004L 47.9997,30.0004L 47.9997,28.0004L 45.9997,28.0004 Z M 48.9997,28.0003L 48.9997,30.0003L 50.9997,30.0003L 50.9997,28.0003L 48.9997,28.0003 Z M 51.9997,28.0003L 51.9997,30.0003L 53.9997,30.0003L 53.9997,28.0003L 51.9997,28.0003 Z M 21.9998,31.0003L 21.9998,33.0003L 23.9998,33.0003L 23.9998,31.0003L 21.9998,31.0003 Z M 24.9998,31.0003L 24.9998,33.0003L 26.9998,33.0003L 26.9998,31.0003L 24.9998,31.0003 Z M 27.9998,31.0003L 27.9998,33.0003L 29.9998,33.0003L 29.9998,31.0003L 27.9998,31.0003 Z M 45.9997,31.0003L 45.9997,33.0003L 47.9997,33.0003L 47.9997,31.0003L 45.9997,31.0003 Z M 48.9997,31.0003L 48.9997,33.0003L 50.9997,33.0003L 50.9997,31.0003L 48.9997,31.0003 Z M 51.9997,31.0003L 51.9997,33.0003L 53.9997,33.0003L 53.9997,31.0003L 51.9997,31.0003 Z M 21.9998,34.0001L 21.9998,36.0001L 23.9998,36.0001L 23.9998,34.0001L 21.9998,34.0001 Z M 24.9999,34.0001L 24.9999,36.0001L 26.9999,36.0001L 26.9999,34.0001L 24.9999,34.0001 Z M 27.9999,34.0001L 27.9999,36.0001L 29.9999,36.0001L 29.9999,34.0001L 27.9999,34.0001 Z M 45.9997,34.0001L 45.9997,36.0001L 47.9997,36.0001L 47.9997,34.0001L 45.9997,34.0001 Z M 48.9997,34.0001L 48.9997,36.0001L 50.9997,36.0001L 50.9997,34.0001L 48.9997,34.0001 Z M 51.9997,34.0001L 51.9997,36.0001L 53.9997,36.0001L 53.9997,34.0001L 51.9997,34.0001 Z M 21.9999,37.0001L 21.9999,39.0001L 23.9999,39.0001L 23.9999,37.0001L 21.9999,37.0001 Z M 24.9999,37.0001L 24.9999,39.0001L 26.9999,39.0001L 26.9999,37.0001L 24.9999,37.0001 Z M 27.9999,37.0001L 27.9999,39.0001L 29.9999,39.0001L 29.9999,37.0001L 27.9999,37.0001 Z M 45.9997,37.0001L 45.9997,39.0001L 47.9997,39.0001L 47.9997,37.0001L 45.9997,37.0001 Z M 48.9998,37.0001L 48.9998,39.0001L 50.9998,39.0001L 50.9998,37.0001L 48.9998,37.0001 Z M 51.9998,37.0001L 51.9998,39.0001L 53.9998,39.0001L 53.9998,37.0001L 51.9998,37.0001 Z M 21.9999,40.0001L 21.9999,42.0001L 23.9999,42.0001L 23.9999,40.0001L 21.9999,40.0001 Z M 24.9999,40.0001L 24.9999,42.0001L 26.9999,42.0001L 26.9999,40.0001L 24.9999,40.0001 Z M 27.9999,40.0001L 27.9999,42.0001L 29.9999,42.0001L 29.9999,40.0001L 27.9999,40.0001 Z M 45.9998,40.0001L 45.9998,42.0001L 47.9998,42.0001L 47.9998,40.0001L 45.9998,40.0001 Z M 48.9998,40.0001L 48.9998,42.0001L 50.9998,42.0001L 50.9998,40.0001L 48.9998,40.0001 Z M 51.9998,40.0001L 51.9998,42.0001L 53.9998,42.0001L 53.9998,40.0001L 51.9998,40.0001 Z M 21.9999,43.0001L 21.9999,45.0001L 23.9999,45.0001L 23.9999,43.0001L 21.9999,43.0001 Z M 24.9999,43.0001L 24.9999,45.0001L 26.9999,45.0001L 26.9999,43.0001L 24.9999,43.0001 Z M 27.9999,43.0001L 27.9999,45.0001L 29.9999,45.0001L 29.9999,43.0001L 27.9999,43.0001 Z M 45.9998,43.0001L 45.9998,45.0001L 47.9998,45.0001L 47.9998,43.0001L 45.9998,43.0001 Z M 48.9998,43.0001L 48.9998,45.0001L 50.9998,45.0001L 50.9998,43.0001L 48.9998,43.0001 Z M 51.9998,43.0001L 51.9998,45.0001L 53.9998,45.0001L 53.9998,43.0001L 51.9998,43.0001 Z M 21.9999,46.0001L 21.9999,48.0001L 23.9999,48.0001L 23.9999,46.0001L 21.9999,46.0001 Z M 24.9999,46.0001L 24.9999,48.0001L 26.9999,48.0001L 26.9999,46.0001L 24.9999,46.0001 Z M 27.9999,46.0001L 27.9999,48.0001L 29.9999,48.0001L 29.9999,46.0001L 27.9999,46.0001 Z M 30.9999,46.0001L 30.9999,48.0001L 32.9999,48.0001L 32.9999,46.0001L 30.9999,46.0001 Z M 33.9999,46.0001L 33.9999,48.0001L 35.9999,48.0001L 35.9999,46.0001L 33.9999,46.0001 Z M 36.9999,46.0001L 36.9999,48.0001L 38.9999,48.0001L 38.9999,46.0001L 36.9999,46.0001 Z M 39.9999,46.0001L 39.9999,48.0001L 41.9999,48.0001L 41.9999,46.0001L 39.9999,46.0001 Z M 42.9999,46.0001L 42.9999,48.0001L 44.9999,48.0001L 44.9999,46.0001L 42.9999,46.0001 Z M 45.9999,46.0001L 45.9999,48.0001L 47.9999,48.0001L 47.9999,46.0001L 45.9999,46.0001 Z M 48.9999,46.0001L 48.9999,48.0001L 50.9999,48.0001L 50.9999,46.0001L 48.9999,46.0001 Z M 51.9999,46.0001L 51.9999,48.0001L 53.9999,48.0001L 53.9999,46.0001L 51.9999,46.0001 Z M 21.9999,49.0001L 21.9999,51.0001L 23.9999,51.0001L 23.9999,49.0001L 21.9999,49.0001 Z M 24.9999,49.0001L 24.9999,51.0001L 26.9999,51.0001L 26.9999,49.0001L 24.9999,49.0001 Z M 27.9999,49.0001L 27.9999,51.0001L 29.9999,51.0001L 29.9999,49.0001L 27.9999,49.0001 Z M 30.9999,49.0001L 30.9999,51.0001L 33,51.0001L 33,49.0001L 30.9999,49.0001 Z M 34,49.0001L 34,51.0001L 36,51.0001L 36,49.0001L 34,49.0001 Z M 37,49.0001L 37,51.0001L 39,51.0001L 39,49.0001L 37,49.0001 Z M 40,49.0001L 40,51.0001L 42,51.0001L 42,49.0001L 40,49.0001 Z M 42.9999,49.0001L 42.9999,51.0001L 44.9999,51.0001L 44.9999,49.0001L 42.9999,49.0001 Z M 45.9999,49L 45.9999,51L 47.9999,51L 47.9999,49L 45.9999,49 Z M 48.9999,49L 48.9999,51L 50.9999,51L 50.9999,49L 48.9999,49 Z M 51.9999,49L 51.9999,51L 53.9999,51L 53.9999,49L 51.9999,49 Z M 22,52L 22,54L 24,54L 24,52L 22,52 Z M 25,52L 25,54L 27,54L 27,52L 25,52 Z M 28,52L 28,54L 30,54L 30,52L 28,52 Z M 31,52L 31,54L 33,54L 33,52L 31,52 Z M 34,52L 34,54L 36,54L 36,52L 34,52 Z M 37,52L 37,54L 39,54L 39,52L 37,52 Z M 40,52L 40,54L 42,54L 42,52L 40,52 Z M 43,52L 43,54L 45,54L 45,52L 43,52 Z M 46,52L 46,54L 48,54L 48,52L 46,52 Z M 49,52L 49,54L 51,54L 51,52L 49,52 Z M 52,52L 52,54L 54,54L 54,52L 52,52 Z M 31,31L 31,45L 45,45L 45,31L 31,31 Z M 33.6375,36.64L 33.4504,36.565L 33.3733,36.375L 33.4504,36.1829L 33.6375,36.1067L 33.8283,36.1829L 33.9067,36.375L 33.8283,36.5625L 33.6375,36.64 Z M 33.8533,40L 33.4266,40L 33.4266,37.3334L 33.8533,37.3334L 33.8533,40 Z M 36.9467,40L 36.52,40L 36.52,38.4942C 36.52,37.9336 36.3092,37.6533 35.8875,37.6533C 35.6697,37.6533 35.4896,37.7328 35.3471,37.8917C 35.2046,38.0506 35.1333,38.2514 35.1333,38.4942L 35.1333,40L 34.7066,40L 34.7066,37.3333L 35.1333,37.3333L 35.1333,37.7992L 35.1441,37.7992C 35.3486,37.4531 35.6444,37.28 36.0317,37.28C 36.3278,37.28 36.5543,37.3739 36.7112,37.5617C 36.8682,37.7495 36.9467,38.0206 36.9467,38.375L 36.9467,40 Z M 39.0267,39.9642L 38.6208,40.0533C 38.1447,40.0533 37.9067,39.7945 37.9067,39.2767L 37.9067,37.7067L 37.4267,37.7067L 37.4267,37.3333L 37.9067,37.3333L 37.9067,36.6733L 38.3333,36.5333L 38.3333,37.3333L 39.0267,37.3333L 39.0267,37.7067L 38.3333,37.7067L 38.3333,39.1892C 38.3333,39.3658 38.3647,39.4918 38.4275,39.5671C 38.4903,39.6424 38.5942,39.68 38.7392,39.68L 39.0267,39.5733L 39.0267,39.9642 Z M 41.6933,38.7733L 39.8267,38.7733C 39.8339,39.0628 39.9142,39.2863 40.0675,39.4438C 40.2208,39.6013 40.4319,39.68 40.7008,39.68C 41.003,39.68 41.2805,39.5911 41.5333,39.4133L 41.5333,39.8042C 41.3,39.9703 40.9911,40.0533 40.6067,40.0533C 40.2311,40.0533 39.9361,39.9331 39.7217,39.6925C 39.5072,39.452 39.4,39.1133 39.4,38.6767C 39.4,38.2645 39.516,37.9286 39.7479,37.6692C 39.9799,37.4097 40.268,37.28 40.6125,37.28C 40.9564,37.28 41.2225,37.3921 41.4108,37.6163C 41.5992,37.8404 41.6933,38.152 41.6933,38.5508L 41.6933,38.7733 Z M 41.2667,38.4C 41.265,38.1645 41.2058,37.9811 41.0892,37.85C 40.9725,37.7189 40.8103,37.6533 40.6025,37.6533C 40.4019,37.6533 40.2317,37.7222 40.0917,37.86C 39.9517,37.9978 39.8653,38.1778 39.8325,38.4L 41.2667,38.4 Z M 42.76,40L 42.3333,40L 42.3333,36.0533L 42.76,36.0533L 42.76,40 Z",
                        config.Hardware,
                        config.Metrics,
                        config.Params,
                        config.Type.GetHardwareTypes()
                        );

                case MonitorType.RAM:
                    return OHMPanel(
                        config.Type,
                        "M 473.00,193.00 C 473.00,193.00 434.00,193.00 434.00,193.00 434.00,193.00 434.00,245.00 434.00,245.00 434.00,245.00 259.00,245.00 259.00,245.00 259.00,239.01 259.59,235.54 256.67,230.00 247.91,213.34 228.26,212.83 217.65,228.00 213.65,233.71 214.00,238.44 214.00,245.00 214.00,245.00 27.00,245.00 27.00,245.00 27.00,245.00 27.00,193.00 27.00,193.00 27.00,193.00 0.00,193.00 0.00,193.00 0.00,193.00 0.00,20.00 0.00,20.00 12.36,19.43 21.26,13.56 18.00,0.00 18.00,0.00 453.00,0.00 453.00,0.00 453.01,7.85 454.03,15.96 463.00,18.82 465.56,19.42 470.18,19.04 473.00,18.82 473.00,18.82 473.00,193.00 473.00,193.00 Z M 433.00,39.00 C 433.00,39.00 386.00,39.00 386.00,39.00 386.00,39.00 386.00,147.00 386.00,147.00 386.00,147.00 433.00,147.00 433.00,147.00 433.00,147.00 433.00,39.00 433.00,39.00 Z M 423.00,193.00 C 423.00,193.00 399.00,193.00 399.00,193.00 399.00,193.00 399.00,224.00 399.00,224.00 399.00,224.00 387.00,224.00 387.00,224.00 387.00,224.00 387.00,193.00 387.00,193.00 387.00,193.00 377.00,193.00 377.00,193.00 377.00,193.00 377.00,224.00 377.00,224.00 377.00,224.00 365.00,224.00 365.00,224.00 365.00,224.00 365.00,193.00 365.00,193.00 365.00,193.00 354.00,193.00 354.00,193.00 354.00,193.00 354.00,224.00 354.00,224.00 354.00,224.00 343.00,224.00 343.00,224.00 343.00,224.00 343.00,193.00 343.00,193.00 343.00,193.00 333.00,193.00 333.00,193.00 333.00,193.00 333.00,224.00 333.00,224.00 333.00,224.00 322.00,224.00 322.00,224.00 322.00,224.00 322.00,193.00 322.00,193.00 322.00,193.00 311.00,193.00 311.00,193.00 311.00,193.00 311.00,224.00 311.00,224.00 311.00,224.00 300.00,224.00 300.00,224.00 300.00,224.00 300.00,193.00 300.00,193.00 300.00,193.00 289.00,193.00 289.00,193.00 289.00,193.00 289.00,224.00 289.00,224.00 289.00,224.00 277.00,224.00 277.00,224.00 277.00,224.00 277.00,193.00 277.00,193.00 277.00,193.00 191.00,193.00 191.00,193.00 191.00,193.00 191.00,224.00 191.00,224.00 191.00,224.00 179.00,224.00 179.00,224.00 179.00,224.00 179.00,193.00 179.00,193.00 179.00,193.00 169.00,193.00 169.00,193.00 169.00,193.00 169.00,224.00 169.00,224.00 169.00,224.00 157.00,224.00 157.00,224.00 157.00,224.00 157.00,193.00 157.00,193.00 157.00,193.00 146.00,193.00 146.00,193.00 146.00,193.00 146.00,224.00 146.00,224.00 146.00,224.00 134.00,224.00 134.00,224.00 134.00,224.00 134.00,193.00 134.00,193.00 134.00,193.00 125.00,193.00 125.00,193.00 125.00,193.00 125.00,224.00 125.00,224.00 125.00,224.00 114.00,224.00 114.00,224.00 114.00,224.00 114.00,193.00 114.00,193.00 114.00,193.00 103.00,193.00 103.00,193.00 103.00,193.00 103.00,224.00 103.00,224.00 103.00,224.00 91.00,224.00 91.00,224.00 91.00,224.00 91.00,193.00 91.00,193.00 91.00,193.00 81.00,193.00 81.00,193.00 81.00,193.00 81.00,224.00 81.00,224.00 81.00,224.00 69.00,224.00 69.00,224.00 69.00,224.00 69.00,193.00 69.00,193.00 69.00,193.00 39.00,193.00 39.00,193.00 39.00,193.00 39.00,234.00 39.00,234.00 39.00,234.00 203.00,234.00 203.00,234.00 204.62,218.32 219.49,205.67 235.00,205.04 245.28,204.62 255.94,209.24 262.67,217.04 265.14,219.89 267.13,223.51 268.54,227.00 269.28,228.84 269.93,231.78 271.56,232.98 273.27,234.24 276.91,234.00 279.00,234.00 279.00,234.00 423.00,234.00 423.00,234.00 423.00,234.00 423.00,193.00 423.00,193.00 Z M 367.00,39.00 C 367.00,39.00 320.00,39.00 320.00,39.00 320.00,39.00 320.00,147.00 320.00,147.00 320.00,147.00 367.00,147.00 367.00,147.00 367.00,147.00 367.00,39.00 367.00,39.00 Z M 303.00,39.00 C 303.00,39.00 256.00,39.00 256.00,39.00 256.00,39.00 256.00,147.00 256.00,147.00 256.00,147.00 303.00,147.00 303.00,147.00 303.00,147.00 303.00,39.00 303.00,39.00 Z M 215.00,39.00 C 215.00,39.00 168.00,39.00 168.00,39.00 168.00,39.00 168.00,147.00 168.00,147.00 168.00,147.00 215.00,147.00 215.00,147.00 215.00,147.00 215.00,39.00 215.00,39.00 Z M 148.00,39.00 C 148.00,39.00 101.00,39.00 101.00,39.00 101.00,39.00 101.00,147.00 101.00,147.00 101.00,147.00 148.00,147.00 148.00,147.00 148.00,147.00 148.00,39.00 148.00,39.00 Z M 84.00,39.00 C 84.00,39.00 37.00,39.00 37.00,39.00 37.00,39.00 37.00,147.00 37.00,147.00 37.00,147.00 84.00,147.00 84.00,147.00 84.00,147.00 84.00,39.00 84.00,39.00 Z",
                        config.Hardware,
                        config.Metrics,
                        config.Params,
                        config.Type.GetHardwareTypes()
                        );

                case MonitorType.GPU:
                    return OHMPanel(
                        config.Type,
                        "F1 M 20,23.0002L 55.9998,23.0002C 57.1044,23.0002 57.9998,23.8956 57.9998,25.0002L 57.9999,46C 57.9999,47.1046 57.1045,48 55.9999,48L 41,48L 41,53L 45,53C 46.1046,53 47,53.8954 47,55L 47,57L 29,57L 29,55C 29,53.8954 29.8955,53 31,53L 35,53L 35,48L 20,48C 18.8954,48 18,47.1046 18,46L 18,25.0002C 18,23.8956 18.8954,23.0002 20,23.0002 Z M 21,26.0002L 21,45L 54.9999,45L 54.9998,26.0002L 21,26.0002 Z",
                        config.Hardware,
                        config.Metrics,
                        config.Params,
                        config.Type.GetHardwareTypes()
                        );

                case MonitorType.HD:
                    return DrivePanel(
                        config.Type,
                        config.Hardware,
                        config.Metrics,
                        config.Params
                        );

                case MonitorType.Network:
                    return NetworkPanel(
                        config.Type,
                        config.Hardware,
                        config.Metrics,
                        config.Params
                        );

                default:
                    throw new ArgumentException("Invalid MonitorType.");
            }
        }

        private MonitorPanel OHMPanel(MonitorType type, string pathData, HardwareConfig[] hardwareConfig, MetricConfig[] metrics, ConfigParam[] parameters, params HardwareType[] hardwareTypes)
        {
            return new MonitorPanel(
                type.GetDescription(),
                pathData,
                OHMMonitor.GetInstances(hardwareConfig, metrics, parameters, type, _board, GetHardware(hardwareTypes).ToArray())
                );
        }

        private MonitorPanel DrivePanel(MonitorType type, HardwareConfig[] hardwareConfig, MetricConfig[] metrics, ConfigParam[] parameters)
        {
            return new MonitorPanel(
                type.GetDescription(),
                "m12.56977,260.69523l0,63.527l352.937,0l0,-63.527l-352.937,0zm232.938,45.881c-7.797,0 -14.118,-6.318 -14.118,-14.117c0,-7.801 6.321,-14.117 14.118,-14.117c7.795,0 14.117,6.316 14.117,14.117c0.001,7.798 -6.322,14.117 -14.117,14.117zm42.353,0c-7.797,0 -14.118,-6.318 -14.118,-14.117c0,-7.801 6.321,-14.117 14.118,-14.117c7.796,0 14.117,6.316 14.117,14.117c0,7.798 -6.321,14.117 -14.117,14.117zm42.352,0c-7.797,0 -14.117,-6.318 -14.117,-14.117c0,-7.801 6.32,-14.117 14.117,-14.117c7.796,0 14.118,6.316 14.118,14.117c0,7.798 -6.323,14.117 -14.118,14.117 M309.0357666015625,52.46223449707031 69.03976440429688,52.46223449707031 12.569778442382812,246.57623291015625 365.50677490234375,246.57623291015625z",
                DriveMonitor.GetInstances(hardwareConfig, metrics, parameters)
                );
        }

        private MonitorPanel NetworkPanel(MonitorType type, HardwareConfig[] hardwareConfig, MetricConfig[] metrics, ConfigParam[] parameters)
        {
            return new MonitorPanel(
                type.GetDescription(),
                "M 40,44L 39.9999,51L 44,51C 45.1046,51 46,51.8954 46,53L 46,57C 46,58.1046 45.1045,59 44,59L 32,59C 30.8954,59 30,58.1046 30,57L 30,53C 30,51.8954 30.8954,51 32,51L 36,51L 36,44L 40,44 Z M 47,53L 57,53L 57,57L 47,57L 47,53 Z M 29,53L 29,57L 19,57L 19,53L 29,53 Z M 19,22L 57,22L 57,31L 19,31L 19,22 Z M 55,24L 53,24L 53,29L 55,29L 55,24 Z M 51,24L 49,24L 49,29L 51,29L 51,24 Z M 47,24L 45,24L 45,29L 47,29L 47,24 Z M 21,27L 21,29L 23,29L 23,27L 21,27 Z M 19,33L 57,33L 57,42L 19,42L 19,33 Z M 55,35L 53,35L 53,40L 55,40L 55,35 Z M 51,35L 49,35L 49,40L 51,40L 51,35 Z M 47,35L 45,35L 45,40L 47,40L 47,35 Z M 21,38L 21,40L 23,40L 23,38L 21,38 Z",
                NetworkMonitor.GetInstances(hardwareConfig, metrics, parameters)
                );
        }

        private void UpdateBoard()
        {
            _board.Update();
        }

        private MonitorPanel[] _monitorPanels { get; set; }

        public MonitorPanel[] MonitorPanels
        {
            get
            {
                return _monitorPanels;
            }
            private set
            {
                _monitorPanels = value;

                NotifyPropertyChanged("MonitorPanels");
            }
        }

        private Computer _computer { get; set; }

        private IHardware _board { get; set; }

        private bool _disposed { get; set; } = false;
    }

    public class MonitorPanel : INotifyPropertyChanged, IDisposable
    {
        public MonitorPanel(string title, string iconData, params iMonitor[] monitors)
        {
            IconPath = Geometry.Parse(iconData);
            Title = title;

            Monitors = monitors;
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (!_disposed)
            {
                if (disposing)
                {
                    foreach (iMonitor _monitor in Monitors)
                    {
                        _monitor.Dispose();
                    }

                    _monitors = null;
                    _iconPath = null;
                }

                _disposed = true;
            }
        }

        ~MonitorPanel()
        {
            Dispose(false);
        }

        public void NotifyPropertyChanged(string propertyName)
        {
            if (PropertyChanged != null)
            {
                PropertyChanged(this, new PropertyChangedEventArgs(propertyName));
            }
        }

        public event PropertyChangedEventHandler PropertyChanged;

        private Geometry _iconPath { get; set; }

        public Geometry IconPath
        {
            get
            {
                return _iconPath;
            }
            private set
            {
                _iconPath = value;

                NotifyPropertyChanged("IconPath");
            }
        }

        private string _title { get; set; }

        public string Title
        {
            get
            {
                return _title;
            }
            private set
            {
                _title = value;

                NotifyPropertyChanged("Title");
            }
        }

        private iMonitor[] _monitors { get; set; }

        public iMonitor[] Monitors
        {
            get
            {
                return _monitors;
            }
            private set
            {
                _monitors = value;

                NotifyPropertyChanged("Monitors");
            }
        }

        private bool _disposed { get; set; } = false;
    }

    public interface iMonitor : INotifyPropertyChanged, IDisposable
    {
        string ID { get; }

        string Name { get; }

        bool ShowName { get; }

        iMetric[] Metrics { get; }

        void Update();
    }

    public class BaseMonitor : iMonitor
    {
        public BaseMonitor(string id, string name, bool showName)
        {
            ID = id;
            Name = name;
            ShowName = showName;
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (!_disposed)
            {
                if (disposing)
                {
                    foreach (iMetric _metric in Metrics)
                    {
                        _metric.Dispose();
                    }

                    _metrics = null;
                }

                _disposed = true;
            }
        }

        ~BaseMonitor()
        {
            Dispose(false);
        }

        public virtual void Update()
        {
            foreach (iMetric _metric in Metrics)
            {
                _metric.Update();
            }
        }

        public void NotifyPropertyChanged(string propertyName)
        {
            if (PropertyChanged != null)
            {
                PropertyChanged(this, new PropertyChangedEventArgs(propertyName));
            }
        }

        public event PropertyChangedEventHandler PropertyChanged;

        private string _id { get; set; }

        public string ID
        {
            get
            {
                return _id;
            }
            protected set
            {
                _id = value;

                NotifyPropertyChanged("ID");
            }
        }

        private string _name { get; set; }

        public string Name
        {
            get
            {
                return _name;
            }
            protected set
            {
                _name = value;

                NotifyPropertyChanged("Name");
            }
        }

        private bool _showName { get; set; }

        public bool ShowName
        {
            get
            {
                return _showName;
            }
            protected set
            {
                _showName = value;

                NotifyPropertyChanged("ShowName");
            }
        }

        private iMetric[] _metrics { get; set; }

        public iMetric[] Metrics
        {
            get
            {
                return _metrics;
            }
            protected set
            {
                _metrics = value;

                NotifyPropertyChanged("Metrics");
            }
        }

        private bool _disposed { get; set; } = false;
    }

    public class OHMMonitor : BaseMonitor
    {
        public OHMMonitor(MonitorType type, string id, string name, IHardware hardware, IHardware board, MetricConfig[] metrics, ConfigParam[] parameters) : base(id, name, parameters.GetValue<bool>(ParamKey.HardwareNames))
        {
            _hardware = hardware;

            UpdateHardware();

            switch (type)
            {
                case MonitorType.CPU:
                    InitCPU(
                        board,
                        metrics,
                        parameters.GetValue<bool>(ParamKey.RoundAll),
                        parameters.GetValue<bool>(ParamKey.AllCoreClocks),
                        parameters.GetValue<bool>(ParamKey.UseGHz),
                        parameters.GetValue<bool>(ParamKey.UseFahrenheit),
                        parameters.GetValue<int>(ParamKey.TempAlert)
                        );
                    break;

                case MonitorType.RAM:
                    InitRAM(
                        board,
                        metrics,
                        parameters.GetValue<bool>(ParamKey.RoundAll)
                        );
                    break;

                case MonitorType.GPU:
                    InitGPU(
                        metrics,
                        parameters.GetValue<bool>(ParamKey.RoundAll),
                        parameters.GetValue<bool>(ParamKey.UseGHz),
                        parameters.GetValue<bool>(ParamKey.UseFahrenheit),
                        parameters.GetValue<int>(ParamKey.TempAlert)
                        );
                    break;

                default:
                    throw new ArgumentException("Invalid MonitorType.");
            }
        }

        public new void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected override void Dispose(bool disposing)
        {
            base.Dispose(disposing);

            if (!_disposed)
            {
                if (disposing)
                {
                    _hardware = null;
                }

                _disposed = true;
            }
        }

        ~OHMMonitor()
        {
            Dispose(false);
        }

        public static iMonitor[] GetInstances(HardwareConfig[] hardwareConfig, MetricConfig[] metrics, ConfigParam[] parameters, MonitorType type, IHardware board, IHardware[] hardware)
        {
            return (
                from hw in hardware
                join c in hardwareConfig on hw.Identifier.ToString() equals c.ID into merged
                from n in merged.DefaultIfEmpty(new HardwareConfig() { ID = hw.Identifier.ToString(), Name = hw.Name, ActualName = hw.Name }).Select(n => { if (n.ActualName != hw.Name) { n.Name = n.ActualName = hw.Name; } return n; })
                where n.Enabled
                orderby n.Order descending, n.Name ascending
                select new OHMMonitor(type, n.ID, n.Name ?? n.ActualName, hw, board, metrics, parameters)
                ).ToArray();
        }

        public override void Update()
        {
            UpdateHardware();

            base.Update();
        }

        private void UpdateHardware()
        {
            _hardware.Update();
        }

        private void InitCPU(IHardware board, MetricConfig[] metrics, bool roundAll, bool allCoreClocks, bool useGHz, bool useFahrenheit, double tempAlert)
        {
            List<OHMMetric> _sensorList = new List<OHMMetric>();

            if (metrics.IsEnabled(MetricKey.CPUClock))
            {
                Regex regex = new Regex(@"^.*(CPU|Core).*#(\d+)$");

                var coreClocks = _hardware.Sensors
                    .Where(s => s.SensorType == SensorType.Clock)
                    .Select(s => new
                    {
                        Match = regex.Match(s.Name),
                        Sensor = s
                    })
                    .Where(s => s.Match.Success)
                    .Select(s => new
                    {
                        Index = int.Parse(s.Match.Groups[2].Value),
                        s.Sensor
                    })
                    .OrderBy(s => s.Index)
                    .ToList();

                if (coreClocks.Count > 0)
                {
                    if (allCoreClocks)
                    {
                        foreach (var coreClock in coreClocks)
                        {
                            _sensorList.Add(new OHMMetric(coreClock.Sensor, MetricKey.CPUClock, DataType.MHz, string.Format("{0} {1}", Resources.CPUCoreClockLabel, coreClock.Index - 1), (useGHz ? false : true), 0, (useGHz ? MHzToGHz.Instance : null)));
                        }
                    }
                    else
                    {
                        ISensor firstClock = coreClocks
                            .Select(s => s.Sensor)
                            .FirstOrDefault();

                        _sensorList.Add(new OHMMetric(firstClock, MetricKey.CPUClock, DataType.MHz, null, (useGHz ? false : true), 0, (useGHz ? MHzToGHz.Instance : null)));
                    }
                }
            }

            if (metrics.IsEnabled(MetricKey.CPUVoltage))
            {
                ISensor _voltage = null;

                if (board != null)
                {
                    _voltage = board.Sensors.Where(s => s.SensorType == SensorType.Voltage && s.Name.Contains("CPU")).FirstOrDefault();
                }

                if (_voltage == null)
                {
                    _voltage = _hardware.Sensors.Where(s => s.SensorType == SensorType.Voltage).FirstOrDefault();
                }

                if (_voltage != null)
                {
                    _sensorList.Add(new OHMMetric(_voltage, MetricKey.CPUVoltage, DataType.Voltage, null, roundAll));
                }
            }

            if (metrics.IsEnabled(MetricKey.CPUTemp))
            {
                ISensor _tempSensor = null;

                _tempSensor = _hardware.Sensors.Where(s => s.SensorType == SensorType.Temperature && s.Name.Contains("CCDs Max (Tdie)")).FirstOrDefault(); // Check for AMD core chiplet dies (CCDs)

                if (board != null && _tempSensor == null)
                {
                    _tempSensor = board.Sensors.Where(s => s.SensorType == SensorType.Temperature && s.Name.Contains("CPU")).FirstOrDefault();
                }

                if (_tempSensor == null)
                {
                    _tempSensor =
                        _hardware.Sensors.Where(s => s.SensorType == SensorType.Temperature && (s.Name == "CPU Package" || s.Name.Contains("Tdie"))).FirstOrDefault() ??
                        _hardware.Sensors.Where(s => s.SensorType == SensorType.Temperature).FirstOrDefault();
                }

                if (_tempSensor != null)
                {
                    _sensorList.Add(new OHMMetric(_tempSensor, MetricKey.CPUTemp, DataType.Celcius, null, roundAll, tempAlert, (useFahrenheit ? CelciusToFahrenheit.Instance : null)));
                }
            }

            if (metrics.IsEnabled(MetricKey.CPUFan))
            {
                ISensor _fanSensor = null;

                if (board != null)
                {
                    _fanSensor = board.Sensors.Where(s => new SensorType[2] { SensorType.Fan, SensorType.Control }.Contains(s.SensorType) && s.Name.Contains("CPU")).FirstOrDefault();
                }

                if (_fanSensor == null)
                {
                    _fanSensor = _hardware.Sensors.Where(s => new SensorType[2] { SensorType.Fan, SensorType.Control }.Contains(s.SensorType)).FirstOrDefault();
                }

                if (_fanSensor != null)
                {
                    _sensorList.Add(new OHMMetric(_fanSensor, MetricKey.CPUFan, DataType.RPM, null, roundAll));
                }
            }

            bool _loadEnabled = metrics.IsEnabled(MetricKey.CPULoad);
            bool _coreLoadEnabled = metrics.IsEnabled(MetricKey.CPUCoreLoad);

            if (_loadEnabled || _coreLoadEnabled)
            {
                ISensor[] _loadSensors = _hardware.Sensors.Where(s => s.SensorType == SensorType.Load).ToArray();

                if (_loadSensors.Length > 0)
                {
                    if (_loadEnabled)
                    {
                        ISensor _totalCPU = _loadSensors.Where(s => s.Index == 0).FirstOrDefault();

                        if (_totalCPU != null)
                        {
                            _sensorList.Add(new OHMMetric(_totalCPU, MetricKey.CPULoad, DataType.Percent, null, roundAll));
                        }
                    }

                    if (_coreLoadEnabled)
                    {
                        for (int i = 1; i <= _loadSensors.Max(s => s.Index); i++)
                        {
                            ISensor _coreLoad = _loadSensors.Where(s => s.Index == i).FirstOrDefault();

                            if (_coreLoad != null)
                            {
                                _sensorList.Add(new OHMMetric(_coreLoad, MetricKey.CPUCoreLoad, DataType.Percent, string.Format("{0} {1}", Resources.CPUCoreLoadLabel, i - 1), roundAll));
                            }
                        }
                    }
                }
            }

            Metrics = _sensorList.ToArray();
        }

        public void InitRAM(IHardware board, MetricConfig[] metrics, bool roundAll)
        {
            List<OHMMetric> _sensorList = new List<OHMMetric>();

            if (metrics.IsEnabled(MetricKey.RAMClock))
            {
                ISensor _ramClock = _hardware.Sensors.Where(s => s.SensorType == SensorType.Clock).FirstOrDefault();

                if (_ramClock != null)
                {
                    _sensorList.Add(new OHMMetric(_ramClock, MetricKey.RAMClock, DataType.MHz, null, true));
                }
            }

            if (metrics.IsEnabled(MetricKey.RAMVoltage))
            {
                ISensor _voltage = null;

                if (board != null)
                {
                    _voltage = board.Sensors.Where(s => s.SensorType == SensorType.Voltage && s.Name.Contains("RAM")).FirstOrDefault();
                }

                if (_voltage == null)
                {
                    _voltage = _hardware.Sensors.Where(s => s.SensorType == SensorType.Voltage).FirstOrDefault();
                }

                if (_voltage != null)
                {
                    _sensorList.Add(new OHMMetric(_voltage, MetricKey.RAMVoltage, DataType.Voltage, null, roundAll));
                }
            }

            if (metrics.IsEnabled(MetricKey.RAMLoad))
            {
                ISensor _loadSensor = _hardware.Sensors.Where(s => s.SensorType == SensorType.Load && s.Index == 0).FirstOrDefault();

                if (_loadSensor != null)
                {
                    _sensorList.Add(new OHMMetric(_loadSensor, MetricKey.RAMLoad, DataType.Percent, null, roundAll));
                }
            }

            if (metrics.IsEnabled(MetricKey.RAMUsed))
            {
                ISensor _usedSensor = _hardware.Sensors.Where(s => s.SensorType == SensorType.Data && s.Index == 0).FirstOrDefault();

                if (_usedSensor != null)
                {
                    _sensorList.Add(new OHMMetric(_usedSensor, MetricKey.RAMUsed, DataType.Gigabyte, null, roundAll));
                }
            }

            if (metrics.IsEnabled(MetricKey.RAMFree))
            {
                ISensor _freeSensor = _hardware.Sensors.Where(s => s.SensorType == SensorType.Data && s.Index == 1).FirstOrDefault();

                if (_freeSensor != null)
                {
                    _sensorList.Add(new OHMMetric(_freeSensor, MetricKey.RAMFree, DataType.Gigabyte, null, roundAll));
                }
            }

            Metrics = _sensorList.ToArray();
        }

        public void InitGPU(MetricConfig[] metrics, bool roundAll, bool useGHz, bool useFahrenheit, double tempAlert)
        {
            List<iMetric> _sensorList = new List<iMetric>();

            if (metrics.IsEnabled(MetricKey.GPUCoreClock))
            {
                ISensor _coreClock = _hardware.Sensors.Where(s => s.SensorType == SensorType.Clock && s.Name.Contains("Core")).FirstOrDefault();

                if (_coreClock != null)
                {
                    _sensorList.Add(new OHMMetric(_coreClock, MetricKey.GPUCoreClock, DataType.MHz, null, (useGHz ? false : true), 0, (useGHz ? MHzToGHz.Instance : null)));
                }
            }

            if (metrics.IsEnabled(MetricKey.GPUVRAMClock))
            {
                ISensor _vramClock = _hardware.Sensors.Where(s => s.SensorType == SensorType.Clock && s.Name.Contains("Memory")).FirstOrDefault();

                if (_vramClock != null)
                {
                    _sensorList.Add(new OHMMetric(_vramClock, MetricKey.GPUVRAMClock, DataType.MHz, null, (useGHz ? false : true), 0, (useGHz ? MHzToGHz.Instance : null)));
                }
            }

            if (metrics.IsEnabled(MetricKey.GPUCoreLoad))
            {
                ISensor _coreLoad = _hardware.Sensors.Where(s => s.SensorType == SensorType.Load && s.Name.Contains("Core")).FirstOrDefault() ??
                    _hardware.Sensors.Where(s => s.SensorType == SensorType.Load && s.Index == 0).FirstOrDefault();

                if (_coreLoad != null)
                {
                    _sensorList.Add(new OHMMetric(_coreLoad, MetricKey.GPUCoreLoad, DataType.Percent, null, roundAll));
                }
            }

            if (metrics.IsEnabled(MetricKey.GPUVRAMLoad))
            {
                ISensor _memoryUsed = _hardware.Sensors.Where(s => (s.SensorType == SensorType.Data || s.SensorType == SensorType.SmallData) && s.Name == "GPU Memory Used").FirstOrDefault();
                ISensor _memoryTotal = _hardware.Sensors.Where(s => (s.SensorType == SensorType.Data || s.SensorType == SensorType.SmallData) && s.Name == "GPU Memory Total").FirstOrDefault();

                if (_memoryUsed != null && _memoryTotal != null)
                {
                    _sensorList.Add(new GPUVRAMMLoadMetric(_memoryUsed, _memoryTotal, MetricKey.GPUVRAMLoad, DataType.Percent, null, roundAll));
                }
                else
                {
                    ISensor _vramLoad = _hardware.Sensors.Where(s => s.SensorType == SensorType.Load && s.Name.Contains("Memory")).FirstOrDefault() ??
                        _hardware.Sensors.Where(s => s.SensorType == SensorType.Load && s.Index == 1).FirstOrDefault();

                    if (_vramLoad != null)
                    {
                        _sensorList.Add(new OHMMetric(_vramLoad, MetricKey.GPUVRAMLoad, DataType.Percent, null, roundAll));
                    }
                }
            }

            if (metrics.IsEnabled(MetricKey.GPUVoltage))
            {
                ISensor _voltage = _hardware.Sensors.Where(s => s.SensorType == SensorType.Voltage && s.Index == 0).FirstOrDefault();

                if (_voltage != null)
                {
                    _sensorList.Add(new OHMMetric(_voltage, MetricKey.GPUVoltage, DataType.Voltage, null, roundAll));
                }
            }

            if (metrics.IsEnabled(MetricKey.GPUTemp))
            {
                ISensor _tempSensor = _hardware.Sensors.Where(s => s.SensorType == SensorType.Temperature && s.Index == 0).FirstOrDefault();

                if (_tempSensor != null)
                {
                    _sensorList.Add(new OHMMetric(_tempSensor, MetricKey.GPUTemp, DataType.Celcius, null, roundAll, tempAlert, (useFahrenheit ? CelciusToFahrenheit.Instance : null)));
                }
            }

            if (metrics.IsEnabled(MetricKey.GPUFan))
            {
                ISensor _fanSensor = _hardware.Sensors.Where(s => s.SensorType == SensorType.Control).OrderBy(s => s.Index).FirstOrDefault();

                if (_fanSensor != null)
                {
                    _sensorList.Add(new OHMMetric(_fanSensor, MetricKey.GPUFan, DataType.Percent));
                }
            }

            Metrics = _sensorList.ToArray();
        }

        private IHardware _hardware { get; set; }

        private bool _disposed { get; set; } = false;
    }

    public class DriveMonitor : BaseMonitor
    {
        private const string CATEGORYNAME = "LogicalDisk";

        private const string FREEMB = "Free Megabytes";
        private const string PERCENTFREE = "% Free Space";
        private const string BYTESREADPERSECOND = "Disk Read Bytes/sec";
        private const string BYTESWRITEPERSECOND = "Disk Write Bytes/sec";

        public DriveMonitor(string id, string name, MetricConfig[] metrics, bool roundAll = false, double usedSpaceAlert = 0) : base(id, name, true)
        {
            _loadEnabled = metrics.IsEnabled(MetricKey.DriveLoad);

            bool _loadBarEnabled = metrics.IsEnabled(MetricKey.DriveLoadBar);
            bool _usedEnabled = metrics.IsEnabled(MetricKey.DriveUsed);
            bool _freeEnabled = metrics.IsEnabled(MetricKey.DriveFree);
            bool _readEnabled = metrics.IsEnabled(MetricKey.DriveRead);
            bool _writeEnabled = metrics.IsEnabled(MetricKey.DriveWrite);

            if (_loadBarEnabled)
            {
                if (metrics.Count(m => m.Enabled) == 1 && new Regex("^[A-Z]:$").IsMatch(name))
                {
                    Status = State.LoadBarInline;
                }
                else
                {
                    Status = State.LoadBarStacked;
                }
            }
            else
            {
                Status = State.NoLoadBar;
            }

            if (_loadBarEnabled || _loadEnabled || _usedEnabled || _freeEnabled)
            {
                _counterFreeMB = new PerformanceCounter(CATEGORYNAME, FREEMB, id);
                _counterFreePercent = new PerformanceCounter(CATEGORYNAME, PERCENTFREE, id);
            }

            List<iMetric> _metrics = new List<iMetric>();

            if (_loadBarEnabled || _loadEnabled)
            {
                LoadMetric = new BaseMetric(MetricKey.DriveLoad, DataType.Percent, null, roundAll, usedSpaceAlert);
                _metrics.Add(LoadMetric);
            }

            if (_usedEnabled)
            {
                UsedMetric = new BaseMetric(MetricKey.DriveUsed, DataType.Gigabyte, null, roundAll);
                _metrics.Add(UsedMetric);
            }

            if (_freeEnabled)
            {
                FreeMetric = new BaseMetric(MetricKey.DriveFree, DataType.Gigabyte, null, roundAll);
                _metrics.Add(FreeMetric);
            }

            if (_readEnabled)
            {
                _metrics.Add(new PCMetric(new PerformanceCounter(CATEGORYNAME, BYTESREADPERSECOND, id), MetricKey.DriveRead, DataType.kBps, null, roundAll, 0, BytesPerSecondConverter.Instance));
            }

            if (_writeEnabled)
            {
                _metrics.Add(new PCMetric(new PerformanceCounter(CATEGORYNAME, BYTESWRITEPERSECOND, id), MetricKey.DriveWrite, DataType.kBps, null, roundAll, 0, BytesPerSecondConverter.Instance));
            }

            Metrics = _metrics.ToArray();
        }

        public new void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected override void Dispose(bool disposing)
        {
            base.Dispose(disposing);

            if (!_disposed)
            {
                if (disposing)
                {
                    if (_loadMetric != null)
                    {
                        _loadMetric.Dispose();
                        _loadMetric = null;
                    }

                    if (_usedMetric != null)
                    {
                        _usedMetric.Dispose();
                        _usedMetric = null;
                    }

                    if (_freeMetric != null)
                    {
                        _freeMetric.Dispose();
                        _freeMetric = null;
                    }

                    if (_counterFreeMB != null)
                    {
                        _counterFreeMB.Dispose();
                        _counterFreeMB = null;
                    }

                    if (_counterFreePercent != null)
                    {
                        _counterFreePercent.Dispose();
                        _counterFreePercent = null;
                    }
                }

                _disposed = true;
            }
        }

        ~DriveMonitor()
        {
            Dispose(false);
        }

        public static IEnumerable<HardwareConfig> GetHardware()
        {
            string[] _instances;

            try
            {
                _instances = new PerformanceCounterCategory(CATEGORYNAME).GetInstanceNames();
            }
            catch (InvalidOperationException)
            {
                _instances = new string[0];

                App.ShowPerformanceCounterError();
            }

            Regex _regex = new Regex("^[A-Z]:$");

            return _instances.Where(n => _regex.IsMatch(n)).OrderBy(d => d[0]).Select(h => new HardwareConfig() { ID = h, Name = h, ActualName = h });
        }

        public static iMonitor[] GetInstances(HardwareConfig[] hardwareConfig, MetricConfig[] metrics, ConfigParam[] parameters)
        {
            bool _roundAll = parameters.GetValue<bool>(ParamKey.RoundAll);
            int _usedSpaceAlert = parameters.GetValue<int>(ParamKey.UsedSpaceAlert);

            return (
                from hw in GetHardware()
                join c in hardwareConfig on hw.ID equals c.ID into merged
                from n in merged.DefaultIfEmpty(hw).Select(n => { n.ActualName = hw.Name; return n; })
                where n.Enabled
                orderby n.Order descending, n.Name ascending
                select new DriveMonitor(n.ID, n.Name ?? n.ActualName, metrics, _roundAll, _usedSpaceAlert)
                ).ToArray();
        }

        public override void Update()
        {
            if (!PerformanceCounterCategory.InstanceExists(ID, CATEGORYNAME))
            {
                return;
            }

            if (_counterFreeMB != null && _counterFreePercent != null)
            {
                double _freeGB = _counterFreeMB.NextValue() / 1024d;
                double _freePercent = _counterFreePercent.NextValue();

                double _usedPercent = 100d - _freePercent;

                double _totalGB = _freeGB / (_freePercent / 100d);
                double _usedGB = _totalGB - _freeGB;

                if (LoadMetric != null)
                {
                    LoadMetric.Update(_usedPercent);
                }

                if (UsedMetric != null)
                {
                    UsedMetric.Update(_usedGB);
                }

                if (FreeMetric != null)
                {
                    FreeMetric.Update(_freeGB);
                }
            }

            base.Update();
        }

        private State _status { get; set; }

        public State Status
        {
            get
            {
                return _status;
            }
            private set
            {
                _status = value;

                NotifyPropertyChanged("Status");
            }
        }

        private iMetric _loadMetric { get; set; }

        public iMetric LoadMetric
        {
            get
            {
                return _loadMetric;
            }
            private set
            {
                _loadMetric = value;

                NotifyPropertyChanged("LoadMetric");
            }
        }

        private iMetric _usedMetric { get; set; }

        public iMetric UsedMetric
        {
            get
            {
                return _usedMetric;
            }
            private set
            {
                _usedMetric = value;

                NotifyPropertyChanged("UsedMetric");
            }
        }

        private iMetric _freeMetric { get; set; }

        public iMetric FreeMetric
        {
            get
            {
                return _freeMetric;
            }
            private set
            {
                _freeMetric = value;

                NotifyPropertyChanged("FreeMetric");
            }
        }

        public iMetric[] DriveMetrics
        {
            get
            {
                if (_loadEnabled)
                {
                    return Metrics;
                }
                else
                {
                    return Metrics.Where(m => m.Key != MetricKey.DriveLoad).ToArray();
                }
            }
        }

        private PerformanceCounter _counterFreeMB { get; set; }

        private PerformanceCounter _counterFreePercent { get; set; }

        private bool _loadEnabled { get; set; }

        private bool _disposed { get; set; } = false;

        public enum State : byte
        {
            NoLoadBar,
            LoadBarInline,
            LoadBarStacked
        }
    }

    public class NetworkMonitor : BaseMonitor
    {
        private const string CATEGORYNAME = "Network Interface";

        private const string BYTESRECEIVEDPERSECOND = "Bytes Received/sec";
        private const string BYTESSENTPERSECOND = "Bytes Sent/sec";

        public NetworkMonitor(string id, string name, string extIP, MetricConfig[] metrics, bool showName = true, bool roundAll = false, bool useBytes = false, double bandwidthInAlert = 0, double bandwidthOutAlert = 0) : base(id, name, showName)
        {
            iConverter _converter;

            if (useBytes)
            {
                _converter = BytesPerSecondConverter.Instance;
            }
            else
            {
                _converter = BitsPerSecondConverter.Instance;
            }

            List<iMetric> _metrics = new List<iMetric>();

            if (metrics.IsEnabled(MetricKey.NetworkIP))
            {
                string _ipAddress = GetAdapterIPAddress(name);

                if (!string.IsNullOrEmpty(_ipAddress))
                {
                    _metrics.Add(new IPMetric(_ipAddress, MetricKey.NetworkIP, DataType.IP));
                }
            }

            if (!string.IsNullOrEmpty(extIP))
            {
                _metrics.Add(new IPMetric(extIP, MetricKey.NetworkExtIP, DataType.IP));
            }

            if (metrics.IsEnabled(MetricKey.NetworkIn))
            {
                _metrics.Add(new PCMetric(new PerformanceCounter(CATEGORYNAME, BYTESRECEIVEDPERSECOND, id), MetricKey.NetworkIn, DataType.kbps, null, roundAll, bandwidthInAlert, _converter));
            }

            if (metrics.IsEnabled(MetricKey.NetworkOut))
            {
                _metrics.Add(new PCMetric(new PerformanceCounter(CATEGORYNAME, BYTESSENTPERSECOND, id), MetricKey.NetworkOut, DataType.kbps, null, roundAll, bandwidthOutAlert, _converter));
            }

            Metrics = _metrics.ToArray();
        }

        ~NetworkMonitor()
        {
            Dispose(false);
        }

        public static IEnumerable<HardwareConfig> GetHardware()
        {
            string[] _instances;

            try
            {
                _instances = new PerformanceCounterCategory(CATEGORYNAME).GetInstanceNames();
            }
            catch (InvalidOperationException)
            {
                _instances = new string[0];

                App.ShowPerformanceCounterError();
            }

            Regex _regex = new Regex(@"^isatap.*$");

            return _instances.Where(i => !_regex.IsMatch(i)).OrderBy(h => h).Select(h => new HardwareConfig() { ID = h, Name = h, ActualName = h });
        }

        public static iMonitor[] GetInstances(HardwareConfig[] hardwareConfig, MetricConfig[] metrics, ConfigParam[] parameters)
        {
            bool _showName = parameters.GetValue<bool>(ParamKey.HardwareNames);
            bool _roundAll = parameters.GetValue<bool>(ParamKey.RoundAll);
            bool _useBytes = parameters.GetValue<bool>(ParamKey.UseBytes);
            int _bandwidthInAlert = parameters.GetValue<int>(ParamKey.BandwidthInAlert);
            int _bandwidthOutAlert = parameters.GetValue<int>(ParamKey.BandwidthOutAlert);

            string _extIP = null;

            if (metrics.IsEnabled(MetricKey.NetworkExtIP))
            {
                _extIP = GetExternalIPAddress();
            }

            return (
                from hw in GetHardware()
                join c in hardwareConfig on hw.ID equals c.ID into merged
                from n in merged.DefaultIfEmpty(hw).Select(n => { n.ActualName = hw.Name; return n; })
                where n.Enabled
                orderby n.Order descending, n.Name ascending
                select new NetworkMonitor(n.ID, n.Name ?? n.ActualName, _extIP, metrics, _showName, _roundAll, _useBytes, _bandwidthInAlert, _bandwidthOutAlert)
                ).ToArray();
        }

        public override void Update()
        {
            if (!PerformanceCounterCategory.InstanceExists(ID, CATEGORYNAME))
            {
                return;
            }

            base.Update();
        }

        private static string GetAdapterIPAddress(string name)
        {
            //Here we need to match the apdapter returned by the network interface to the
            //adapter represented by this instance of the class.

            string configuredName = Regex.Replace(name, @"[^\w\d\s]", "");

            foreach (NetworkInterface netif in NetworkInterface.GetAllNetworkInterfaces())
            {
                //Strange pattern matching as the Performance Monitor routines which provide the ID and Names
                //instantiating this class return different values for the devices than the NetworkInterface calls used here.
                //For example Performance Monitor routines return Intel[R] where as NetworkInterface returns Intel(R) causing the
                //strings not to match.  So to get around this, use Regex to strip off the special characters and just compare the string values.
                //Also, in some cases the values for Description match the Performance Monitor calls, and 
                //in others the Name is what matches.  It's a little weird, but this will pick up all 4 network adapters on 
                //my test machine correctly.

                string interfaceDesc = Regex.Replace(netif.Description, @"[^\w\d\s]", "");
                string interfaceName = Regex.Replace(netif.Name, @"[^\w\d\s]", "");

                if (interfaceDesc == configuredName || interfaceName == configuredName)
                {
                    IPInterfaceProperties properties = netif.GetIPProperties();

                    foreach (IPAddressInformation unicast in properties.UnicastAddresses)
                    {
                        if (unicast.Address.AddressFamily == AddressFamily.InterNetwork)
                        {
                            return unicast.Address.ToString();
                        }
                    }
                }
            }

            return null;
        }

        private static string GetExternalIPAddress()
        {
            try
            {
                HttpWebRequest _request = WebRequest.CreateHttp(Constants.URLs.IPIFY);
                _request.Method = HttpMethod.Get.Method;
                _request.Timeout = 5000;

                using (HttpWebResponse _response = (HttpWebResponse)_request.GetResponse())
                {
                    using (Stream _stream = _response.GetResponseStream())
                    {
                        using (StreamReader _reader = new StreamReader(_stream))
                        {
                            return _reader.ReadToEnd();
                        }
                    }
                }
            }
            catch (WebException)
            {
                return "";
            }
        }
    }

    public interface iMetric : INotifyPropertyChanged, IDisposable
    {
        MetricKey Key { get; }

        string FullName { get; }

        string Label { get; }

        double Value { get; }

        string Append { get; }

        double nValue { get; }

        string nAppend { get; }

        string Text { get; }

        bool IsAlert { get; }

        bool IsNumeric { get; }

        void Update();

        void Update(double value);
    }

    public class BaseMetric : iMetric
    {
        public BaseMetric(MetricKey key, DataType dataType, string label = null, bool round = false, double alertValue = 0, iConverter converter = null)
        {
            _converter = converter;
            _round = round;
            _alertValue = alertValue;

            Key = key;

            if (label == null)
            {
                FullName = key.GetFullName();
                Label = key.GetLabel();
            }
            else
            {
                FullName = Label = label;
            }

            nAppend = Append = converter == null ? dataType.GetAppend() : converter.TargetType.GetAppend();
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (!_disposed)
            {
                if (disposing)
                {
                    if (_alertColorTimer != null)
                    {
                        _alertColorTimer.Stop();
                        _alertColorTimer = null;
                    }

                    _converter = null;
                }

                _disposed = true;
            }
        }

        ~BaseMetric()
        {
            Dispose(false);
        }

        public virtual void Update() { }

        public void Update(double value)
        {
            double _val = value;

            if (_converter == null)
            {
                nValue = _val;
            }
            else if (_converter.IsDynamic)
            {
                double _nVal;
                DataType _dataType;

                _converter.Convert(ref _val, out _nVal, out _dataType);

                nValue = _nVal;
                Append = _dataType.GetAppend();
            }
            else
            {
                _converter.Convert(ref _val);

                nValue = _val;
            }

            Value = _val;

            if (_alertValue > 0 && _alertValue <= nValue)
            {
                if (!IsAlert)
                {
                    IsAlert = true;
                }
            }
            else if (IsAlert)
            {
                IsAlert = false;
            }

            Text = string.Format(
                "{0:#,##0.##}{1}",
                _val.Round(_round),
                Append
                );
        }

        public void NotifyPropertyChanged(string propertyName)
        {
            if (PropertyChanged != null)
            {
                PropertyChanged(this, new PropertyChangedEventArgs(propertyName));
            }
        }

        public event PropertyChangedEventHandler PropertyChanged;

        private MetricKey _key { get; set; }

        public MetricKey Key
        {
            get
            {
                return _key;
            }
            protected set
            {
                _key = value;

                NotifyPropertyChanged("Key");
            }
        }

        private string _fullName { get; set; }

        public string FullName
        {
            get
            {
                return _fullName;
            }
            protected set
            {
                _fullName = value;

                NotifyPropertyChanged("FullName");
            }
        }

        private string _label { get; set; }

        public string Label
        {
            get
            {
                return _label;
            }
            protected set
            {
                _label = value;

                NotifyPropertyChanged("Label");
            }
        }

        private double _value { get; set; }

        public double Value
        {
            get
            {
                return _value;
            }
            protected set
            {
                _value = value;

                NotifyPropertyChanged("Value");
            }
        }

        private string _append { get; set; }

        public string Append
        {
            get
            {
                return _append;
            }
            protected set
            {
                _append = value;

                NotifyPropertyChanged("Append");
            }
        }

        private double _nValue { get; set; }

        public double nValue
        {
            get
            {
                return _nValue;
            }
            set
            {
                _nValue = value;

                NotifyPropertyChanged("nValue");
            }
        }

        private string _nAppend { get; set; }

        public string nAppend
        {
            get
            {
                return _nAppend;
            }
            set
            {
                _nAppend = value;

                NotifyPropertyChanged("nAppend");
            }
        }

        private string _text { get; set; }

        public string Text
        {
            get
            {
                return _text;
            }
            protected set
            {
                _text = value;

                NotifyPropertyChanged("Text");
            }
        }

        private bool _isAlert { get; set; }

        public bool IsAlert
        {
            get
            {
                return _isAlert;
            }
            protected set
            {
                _isAlert = value;

                NotifyPropertyChanged("IsAlert");

                if (value)
                {
                    _alertColorFlag = false;

                    if (Framework.Settings.Instance.AlertBlink)
                    {
                        _alertColorTimer = new DispatcherTimer(DispatcherPriority.Normal, App.Current.Dispatcher);
                        _alertColorTimer.Interval = TimeSpan.FromSeconds(0.5d);
                        _alertColorTimer.Tick += new EventHandler(AlertColorTimer_Tick);
                        _alertColorTimer.Start();
                    }
                }
                else if (_alertColorTimer != null)
                {
                    _alertColorTimer.Stop();
                    _alertColorTimer = null;
                }
            }
        }

        public virtual bool IsNumeric
        {
            get { return true; }
        }

        public string AlertColor
        {
            get
            {
                return _alertColorFlag ? Framework.Settings.Instance.FontColor : Framework.Settings.Instance.AlertFontColor;
            }
        }

        private DispatcherTimer _alertColorTimer;

        private void AlertColorTimer_Tick(object sender, EventArgs e)
        {
            _alertColorFlag = !_alertColorFlag;

            NotifyPropertyChanged("AlertColor");
        }

        private bool _alertColorFlag = false;

        protected iConverter _converter { get; set; }

        protected bool _round { get; set; }

        protected double _alertValue { get; set; }

        private bool _disposed { get; set; } = false;
    }

    public class OHMMetric : BaseMetric
    {
        public OHMMetric(ISensor sensor, MetricKey key, DataType dataType, string label = null, bool round = false, double alertValue = 0, iConverter converter = null) : base(key, dataType, label, round, alertValue, converter)
        {
            _sensor = sensor;
        }

        public new void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected override void Dispose(bool disposing)
        {
            base.Dispose(disposing);

            if (!_disposed)
            {
                if (disposing)
                {
                    _sensor = null;
                }

                _disposed = true;
            }
        }

        ~OHMMetric()
        {
            Dispose(false);
        }

        public override void Update()
        {
            if (_sensor.Value.HasValue)
            {
                Update(_sensor.Value.Value);
            }
            else
            {
                Text = "No Value";
            }
        }

        private ISensor _sensor { get; set; }

        private bool _disposed { get; set; } = false;
    }

    public class GPUVRAMMLoadMetric : BaseMetric
    {
        public GPUVRAMMLoadMetric(ISensor memoryUsedSensor, ISensor memoryTotalSensor, MetricKey key, DataType dataType, string label = null, bool round = false, double alertValue = 0, iConverter converter = null) : base(key, dataType, label, round, alertValue, converter)
        {
            _memoryUsedSensor = memoryUsedSensor;
            _memoryTotalSensor = memoryTotalSensor;
        }

        public new void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected override void Dispose(bool disposing)
        {
            base.Dispose(disposing);

            if (!_disposed)
            {
                if (disposing)
                {
                    _memoryUsedSensor = null;
                    _memoryTotalSensor = null;
                }

                _disposed = true;
            }
        }

        ~GPUVRAMMLoadMetric()
        {
            Dispose(false);
        }

        public override void Update()
        {
            if (_memoryUsedSensor.Value.HasValue && _memoryTotalSensor.Value.HasValue)
            {
                float load = _memoryUsedSensor.Value.Value / _memoryTotalSensor.Value.Value * 100f;

                Update(load);
            }
            else
            {
                Text = "No Value";
            }
        }

        private ISensor _memoryUsedSensor { get; set; }

        private ISensor _memoryTotalSensor { get; set; }

        private bool _disposed { get; set; } = false;
    }

    public class IPMetric : BaseMetric
    {
        public IPMetric(string ipAddress, MetricKey key, DataType dataType, string label = null, bool round = false, double alertValue = 0, iConverter converter = null) : base(key, dataType, label, round, alertValue, converter)
        {
            Text = ipAddress;
        }

        public new void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        ~IPMetric()
        {
            Dispose(false);
        }

        public override bool IsNumeric
        {
            get { return false; }
        }
    }

    public class PCMetric : BaseMetric
    {
        public PCMetric(PerformanceCounter counter, MetricKey key, DataType dataType, string label = null, bool round = false, double alertValue = 0, iConverter converter = null) : base(key, dataType, label, round, alertValue, converter)
        {
            _counter = counter;
        }

        public new void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected override void Dispose(bool disposing)
        {
            base.Dispose(disposing);

            if (!_disposed)
            {
                if (disposing)
                {
                    if (_counter != null)
                    {
                        _counter.Dispose();
                        _counter = null;
                    }
                }

                _disposed = true;
            }
        }

        ~PCMetric()
        {
            Dispose(false);
        }

        public override void Update()
        {
            Update(_counter.NextValue());
        }

        private PerformanceCounter _counter { get; set; }

        private bool _disposed { get; set; } = false;
    }

    [Serializable]
    public enum MonitorType : byte
    {
        CPU,
        RAM,
        GPU,
        HD,
        Network
    }

    [JsonObject(MemberSerialization.OptIn)]
    public class MonitorConfig : INotifyPropertyChanged, ICloneable
    {
        public void NotifyPropertyChanged(string propertyName)
        {
            if (PropertyChanged != null)
            {
                PropertyChanged(this, new PropertyChangedEventArgs(propertyName));
            }
        }

        public event PropertyChangedEventHandler PropertyChanged;

        public MonitorConfig Clone()
        {
            MonitorConfig _clone = (MonitorConfig)MemberwiseClone();
            _clone.Hardware = _clone.Hardware.Select(h => h.Clone()).ToArray();
            _clone.Params = _clone.Params.Select(p => p.Clone()).ToArray();

            if (_clone.HardwareOC != null)
            {
                _clone.HardwareOC = new ObservableCollection<HardwareConfig>(_clone.HardwareOC.Select(h => h.Clone()));
            }

            return _clone;
        }

        object ICloneable.Clone()
        {
            return Clone();
        }

        private MonitorType _type { get; set; }

        [JsonProperty]
        public MonitorType Type
        {
            get
            {
                return _type;
            }
            set
            {
                _type = value;

                NotifyPropertyChanged("Type");
            }
        }

        private bool _enabled { get; set; }

        [JsonProperty]
        public bool Enabled
        {
            get
            {
                return _enabled;
            }
            set
            {
                _enabled = value;

                NotifyPropertyChanged("Enabled");
            }
        }

        private byte _order { get; set; }

        [JsonProperty]
        public byte Order
        {
            get
            {
                return _order;
            }
            set
            {
                _order = value;

                NotifyPropertyChanged("Order");
            }
        }

        private HardwareConfig[] _hardware { get; set; }

        [JsonProperty]
        public HardwareConfig[] Hardware
        {
            get
            {
                return _hardware;
            }
            set
            {
                _hardware = value;

                NotifyPropertyChanged("Hardware");
            }
        }

        private ObservableCollection<HardwareConfig> _hardwareOC { get; set; }

        public ObservableCollection<HardwareConfig> HardwareOC
        {
            get
            {
                return _hardwareOC;
            }
            set
            {
                _hardwareOC = value;

                NotifyPropertyChanged("HardwareOC");
            }
        }

        private MetricConfig[] _metrics { get; set; }

        [JsonProperty]
        public MetricConfig[] Metrics
        {
            get
            {
                return _metrics;
            }
            set
            {
                _metrics = value;

                NotifyPropertyChanged("Metrics");
            }
        }

        private ConfigParam[] _params { get; set; }

        [JsonProperty]
        public ConfigParam[] Params
        {
            get
            {
                return _params;
            }
            set
            {
                _params = value;

                NotifyPropertyChanged("Params");
            }
        }

        public string Name
        {
            get
            {
                return Type.GetDescription();
            }
        }

        public static MonitorConfig[] CheckConfig(MonitorConfig[] config)
        {
            MonitorConfig[] _default = Default;

            if (config == null)
            {
                return _default;
            }

            config = (
                from def in _default
                join rec in config on def.Type equals rec.Type into merged
                from newrec in merged.DefaultIfEmpty(def)
                select newrec
                ).ToArray();

            foreach (MonitorConfig _record in config)
            {
                MonitorConfig _defaultRecord = _default.Single(d => d.Type == _record.Type);

                if (_record.Hardware == null)
                {
                    _record.Hardware = _defaultRecord.Hardware;
                }

                if (_record.Metrics == null)
                {
                    _record.Metrics = _defaultRecord.Metrics;
                }
                else
                {
                    _record.Metrics = (
                        from def in _defaultRecord.Metrics
                        join metric in _record.Metrics on def.Key equals metric.Key into merged
                        from newmetric in merged.DefaultIfEmpty(def)
                        select newmetric
                        ).ToArray();
                }

                if (_record.Params == null)
                {
                    _record.Params = _defaultRecord.Params;
                }
                else
                {
                    _record.Params = (
                        from def in _defaultRecord.Params
                        join param in _record.Params on def.Key equals param.Key into merged
                        from newparam in merged.DefaultIfEmpty(def)
                        select newparam
                        ).ToArray();
                }
            }

            return config;
        }

        public static MonitorConfig[] Default
        {
            get
            {
                return new MonitorConfig[5]
                {
                    new MonitorConfig()
                    {
                        Type = MonitorType.CPU,
                        Enabled = true,
                        Order = 5,
                        Hardware = new HardwareConfig[0],
                        Metrics = new MetricConfig[6]
                        {
                            new MetricConfig(MetricKey.CPUClock, true),
                            new MetricConfig(MetricKey.CPUTemp, true),
                            new MetricConfig(MetricKey.CPUVoltage, true),
                            new MetricConfig(MetricKey.CPUFan, true),
                            new MetricConfig(MetricKey.CPULoad, true),
                            new MetricConfig(MetricKey.CPUCoreLoad, true)
                        },
                        Params = new ConfigParam[6]
                        {
                            ConfigParam.Defaults.HardwareNames,
                            ConfigParam.Defaults.RoundAll,
                            ConfigParam.Defaults.AllCoreClocks,
                            ConfigParam.Defaults.UseGHz,
                            ConfigParam.Defaults.UseFahrenheit,
                            ConfigParam.Defaults.TempAlert
                        }
                    },
                    new MonitorConfig()
                    {
                        Type = MonitorType.RAM,
                        Enabled = true,
                        Order = 4,
                        Hardware = new HardwareConfig[0],
                        Metrics = new MetricConfig[5]
                        {
                            new MetricConfig(MetricKey.RAMClock, true),
                            new MetricConfig(MetricKey.RAMVoltage, true),
                            new MetricConfig(MetricKey.RAMLoad, true),
                            new MetricConfig(MetricKey.RAMUsed, true),
                            new MetricConfig(MetricKey.RAMFree, true)
                        },
                        Params = new ConfigParam[2]
                        {
                            ConfigParam.Defaults.NoHardwareNames,
                            ConfigParam.Defaults.RoundAll
                        }
                    },
                    new MonitorConfig()
                    {
                        Type = MonitorType.GPU,
                        Enabled = true,
                        Order = 3,
                        Hardware = new HardwareConfig[0],
                        Metrics = new MetricConfig[7]
                        {
                            new MetricConfig(MetricKey.GPUCoreClock, true),
                            new MetricConfig(MetricKey.GPUVRAMClock, true),
                            new MetricConfig(MetricKey.GPUCoreLoad, true),
                            new MetricConfig(MetricKey.GPUVRAMLoad, true),
                            new MetricConfig(MetricKey.GPUVoltage, true),
                            new MetricConfig(MetricKey.GPUTemp, true),
                            new MetricConfig(MetricKey.GPUFan, true)
                        },
                        Params = new ConfigParam[5]
                        {
                            ConfigParam.Defaults.HardwareNames,
                            ConfigParam.Defaults.RoundAll,
                            ConfigParam.Defaults.UseGHz,
                            ConfigParam.Defaults.UseFahrenheit,
                            ConfigParam.Defaults.TempAlert
                        }
                    },
                    new MonitorConfig()
                    {
                        Type = MonitorType.HD,
                        Enabled = true,
                        Order = 2,
                        Hardware = new HardwareConfig[0],
                        Metrics = new MetricConfig[6]
                        {
                            new MetricConfig(MetricKey.DriveLoadBar, true),
                            new MetricConfig(MetricKey.DriveLoad, true),
                            new MetricConfig(MetricKey.DriveUsed, true),
                            new MetricConfig(MetricKey.DriveFree, true),
                            new MetricConfig(MetricKey.DriveRead, true),
                            new MetricConfig(MetricKey.DriveWrite, true)
                        },
                        Params = new ConfigParam[2]
                        {
                            ConfigParam.Defaults.RoundAll,
                            ConfigParam.Defaults.UsedSpaceAlert
                        }
                    },
                    new MonitorConfig()
                    {
                        Type = MonitorType.Network,
                        Enabled = true,
                        Order = 1,
                        Hardware = new HardwareConfig[0],
                        Metrics = new MetricConfig[4]
                        {
                            new MetricConfig(MetricKey.NetworkIP, true),
                            new MetricConfig(MetricKey.NetworkExtIP, false),
                            new MetricConfig(MetricKey.NetworkIn, true),
                            new MetricConfig(MetricKey.NetworkOut, true)
                        },
                        Params = new ConfigParam[5]
                        {
                            ConfigParam.Defaults.HardwareNames,
                            ConfigParam.Defaults.RoundAll,
                            ConfigParam.Defaults.UseBytes,
                            ConfigParam.Defaults.BandwidthInAlert,
                            ConfigParam.Defaults.BandwidthOutAlert
                        }
                    }
                };
            }
        }
    }

    [JsonObject(MemberSerialization.OptIn)]
    public class HardwareConfig : INotifyPropertyChanged, ICloneable
    {
        public void NotifyPropertyChanged(string propertyName)
        {
            if (PropertyChanged != null)
            {
                PropertyChanged(this, new PropertyChangedEventArgs(propertyName));
            }
        }

        public event PropertyChangedEventHandler PropertyChanged;

        public HardwareConfig Clone()
        {
            return (HardwareConfig)MemberwiseClone();
        }

        object ICloneable.Clone()
        {
            return Clone();
        }

        private string _id { get; set; }

        [JsonProperty]
        public string ID
        {
            get
            {
                return _id;
            }
            set
            {
                _id = value;

                NotifyPropertyChanged("ID");
            }
        }

        private string _name { get; set; }

        [JsonProperty]
        public string Name
        {
            get
            {
                return _name;
            }
            set
            {
                _name = value;

                NotifyPropertyChanged("Name");
            }
        }

        private string _actualName { get; set; }

        [JsonProperty]
        public string ActualName
        {
            get
            {
                return _actualName;
            }
            set
            {
                _actualName = value;

                NotifyPropertyChanged("ActualName");
            }
        }

        private bool _enabled { get; set; } = true;

        [JsonProperty]
        public bool Enabled
        {
            get
            {
                return _enabled;
            }
            set
            {
                _enabled = value;

                NotifyPropertyChanged("Enabled");
            }
        }

        private byte _order { get; set; } = 0;

        [JsonProperty]
        public byte Order
        {
            get
            {
                return _order;
            }
            set
            {
                _order = value;

                NotifyPropertyChanged("Order");
            }
        }
    }

    [JsonObject(MemberSerialization.OptIn)]
    public class MetricConfig : INotifyPropertyChanged, ICloneable
    {
        public MetricConfig() { }

        public MetricConfig(MetricKey key, bool enabled)
        {
            Key = key;
            Enabled = enabled;
        }

        public void NotifyPropertyChanged(string propertyName)
        {
            if (PropertyChanged != null)
            {
                PropertyChanged(this, new PropertyChangedEventArgs(propertyName));
            }
        }

        public event PropertyChangedEventHandler PropertyChanged;

        public ConfigParam Clone()
        {
            return (ConfigParam)MemberwiseClone();
        }

        object ICloneable.Clone()
        {
            return Clone();
        }

        private MetricKey _key { get; set; }

        [JsonProperty]
        public MetricKey Key
        {
            get
            {
                return _key;
            }
            set
            {
                _key = value;

                NotifyPropertyChanged("Key");
            }
        }

        private bool _enabled { get; set; }

        [JsonProperty]
        public bool Enabled
        {
            get
            {
                return _enabled;
            }
            set
            {
                if (_enabled == value)
                {
                    return;
                }

                _enabled = value;

                NotifyPropertyChanged("Enabled");
            }
        }

        public string Name
        {
            get
            {
                return Key.GetFullName();
            }
        }
    }

    [Serializable]
    public enum MetricKey : byte
    {
        CPUClock = 0,
        CPUTemp = 1,
        CPUVoltage = 2,
        CPUFan = 3,
        CPULoad = 4,
        CPUCoreLoad = 5,

        RAMClock = 6,
        RAMVoltage = 7,
        RAMLoad = 8,
        RAMUsed = 9,
        RAMFree = 10,

        GPUCoreClock = 11,
        GPUVRAMClock = 12,
        GPUCoreLoad = 13,
        GPUVRAMLoad = 14,
        GPUVoltage = 15,
        GPUTemp = 16,
        GPUFan = 17,

        NetworkIP = 26,
        NetworkExtIP = 27,
        NetworkIn = 18,
        NetworkOut = 19,

        DriveLoadBar = 20,
        DriveLoad = 21,
        DriveUsed = 22,
        DriveFree = 23,
        DriveRead = 24,
        DriveWrite = 25
    }

    [JsonObject(MemberSerialization.OptIn)]
    public class ConfigParam : INotifyPropertyChanged, ICloneable
    {
        public void NotifyPropertyChanged(string propertyName)
        {
            if (PropertyChanged != null)
            {
                PropertyChanged(this, new PropertyChangedEventArgs(propertyName));
            }
        }

        public event PropertyChangedEventHandler PropertyChanged;

        public ConfigParam Clone()
        {
            return (ConfigParam)MemberwiseClone();
        }

        object ICloneable.Clone()
        {
            return Clone();
        }

        private ParamKey _key { get; set; }

        [JsonProperty]
        public ParamKey Key
        {
            get
            {
                return _key;
            }
            set
            {
                _key = value;

                NotifyPropertyChanged("Key");
            }
        }

        private object _value { get; set; }

        [JsonProperty]
        public object Value
        {
            get
            {
                return _value;
            }
            set
            {
                if (value.GetType() == typeof(long))
                {
                    _value = Convert.ToInt32(value);
                }
                else
                {
                    _value = value;
                }

                NotifyPropertyChanged("Value");
            }
        }

        public Type Type
        {
            get
            {
                return Value.GetType();
            }
        }

        public string TypeString
        {
            get
            {
                return Type.ToString();
            }
        }

        public string Name
        {
            get
            {
                switch (Key)
                {
                    case ParamKey.HardwareNames:
                        return Resources.SettingsShowHardwareNames;

                    case ParamKey.UseFahrenheit:
                        return Resources.SettingsUseFahrenheit;

                    case ParamKey.AllCoreClocks:
                        return Resources.SettingsAllCoreClocks;

                    case ParamKey.CoreLoads:
                        return Resources.SettingsCoreLoads;

                    case ParamKey.TempAlert:
                        return Resources.SettingsTemperatureAlert;

                    case ParamKey.DriveDetails:
                        return Resources.SettingsShowDriveDetails;

                    case ParamKey.UsedSpaceAlert:
                        return Resources.SettingsUsedSpaceAlert;

                    case ParamKey.BandwidthInAlert:
                        return Resources.SettingsBandwidthInAlert;

                    case ParamKey.BandwidthOutAlert:
                        return Resources.SettingsBandwidthOutAlert;

                    case ParamKey.UseBytes:
                        return Resources.SettingsUseBytesPerSecond;

                    case ParamKey.RoundAll:
                        return Resources.SettingsRoundAllDecimals;

                    case ParamKey.DriveSpace:
                        return Resources.SettingsShowDriveSpace;

                    case ParamKey.DriveIO:
                        return Resources.SettingsShowDriveIO;

                    case ParamKey.UseGHz:
                        return Resources.SettingsUseGHz;

                    default:
                        return "Unknown";
                }
            }
        }

        public string Tooltip
        {
            get
            {
                switch (Key)
                {
                    case ParamKey.HardwareNames:
                        return Resources.SettingsShowHardwareNamesTooltip;

                    case ParamKey.UseFahrenheit:
                        return Resources.SettingsUseFahrenheitTooltip;

                    case ParamKey.AllCoreClocks:
                        return Resources.SettingsAllCoreClocksTooltip;

                    case ParamKey.CoreLoads:
                        return Resources.SettingsCoreLoadsTooltip;

                    case ParamKey.TempAlert:
                        return Resources.SettingsTemperatureAlertTooltip;

                    case ParamKey.DriveDetails:
                        return Resources.SettingsDriveDetailsTooltip;

                    case ParamKey.UsedSpaceAlert:
                        return Resources.SettingsUsedSpaceAlertTooltip;

                    case ParamKey.BandwidthInAlert:
                        return Resources.SettingsBandwidthInAlertTooltip;

                    case ParamKey.BandwidthOutAlert:
                        return Resources.SettingsBandwidthOutAlertTooltip;

                    case ParamKey.UseBytes:
                        return Resources.SettingsUseBytesPerSecondTooltip;

                    case ParamKey.RoundAll:
                        return Resources.SettingsRoundAllDecimalsTooltip;

                    case ParamKey.DriveSpace:
                        return Resources.SettingsShowDriveSpaceTooltip;

                    case ParamKey.DriveIO:
                        return Resources.SettingsShowDriveIOTooltip;

                    case ParamKey.UseGHz:
                        return Resources.SettingsUseGHzTooltip;

                    default:
                        return "Unknown";
                }
            }
        }

        public static class Defaults
        {
            public static ConfigParam HardwareNames
            {
                get
                {
                    return new ConfigParam() { Key = ParamKey.HardwareNames, Value = true };
                }
            }

            public static ConfigParam NoHardwareNames
            {
                get
                {
                    return new ConfigParam() { Key = ParamKey.HardwareNames, Value = false };
                }
            }

            public static ConfigParam UseFahrenheit
            {
                get
                {
                    return new ConfigParam() { Key = ParamKey.UseFahrenheit, Value = false };
                }
            }

            public static ConfigParam AllCoreClocks
            {
                get
                {
                    return new ConfigParam() { Key = ParamKey.AllCoreClocks, Value = false };
                }
            }

            public static ConfigParam CoreLoads
            {
                get
                {
                    return new ConfigParam() { Key = ParamKey.CoreLoads, Value = true };
                }
            }

            public static ConfigParam TempAlert
            {
                get
                {
                    return new ConfigParam() { Key = ParamKey.TempAlert, Value = 0 };
                }
            }

            public static ConfigParam DriveDetails
            {
                get
                {
                    return new ConfigParam() { Key = ParamKey.DriveDetails, Value = false };
                }
            }

            public static ConfigParam UsedSpaceAlert
            {
                get
                {
                    return new ConfigParam() { Key = ParamKey.UsedSpaceAlert, Value = 0 };
                }
            }

            public static ConfigParam BandwidthInAlert
            {
                get
                {
                    return new ConfigParam() { Key = ParamKey.BandwidthInAlert, Value = 0 };
                }
            }

            public static ConfigParam BandwidthOutAlert
            {
                get
                {
                    return new ConfigParam() { Key = ParamKey.BandwidthOutAlert, Value = 0 };
                }
            }

            public static ConfigParam UseBytes
            {
                get
                {
                    return new ConfigParam() { Key = ParamKey.UseBytes, Value = false };
                }
            }

            public static ConfigParam RoundAll
            {
                get
                {
                    return new ConfigParam() { Key = ParamKey.RoundAll, Value = false };
                }
            }

            public static ConfigParam ShowDriveSpace
            {
                get
                {
                    return new ConfigParam() { Key = ParamKey.DriveSpace, Value = true };
                }
            }

            public static ConfigParam ShowDriveIO
            {
                get
                {
                    return new ConfigParam() { Key = ParamKey.DriveIO, Value = true };
                }
            }

            public static ConfigParam UseGHz
            {
                get
                {
                    return new ConfigParam() { Key = ParamKey.UseGHz, Value = false };
                }
            }
        }
    }

    [Serializable]
    public enum ParamKey : byte
    {
        HardwareNames,
        UseFahrenheit,
        AllCoreClocks,
        CoreLoads,
        TempAlert,
        DriveDetails,
        UsedSpaceAlert,
        BandwidthInAlert,
        BandwidthOutAlert,
        UseBytes,
        RoundAll,
        DriveSpace,
        DriveIO,
        UseGHz
    }

    public enum DataType : byte
    {
        Dynamic,
        Bit,
        Kilobit,
        Megabit,
        Gigabit,
        Byte,
        Kilobyte,
        Megabyte,
        Gigabyte,
        bps,
        kbps,
        Mbps,
        Gbps,
        Bps,
        kBps,
        MBps,
        GBps,
        MHz,
        GHz,
        Voltage,
        Percent,
        RPM,
        Celcius,
        Fahrenheit,
        IP
    }

    public interface iConverter
    {
        void Convert(ref double value);

        void Convert(ref double value, out double normalized, out DataType targetType);

        DataType TargetType { get; }

        bool IsDynamic { get; }
    }

    public class CelciusToFahrenheit : iConverter
    {
        private CelciusToFahrenheit() { }

        public void Convert(ref double value)
        {
            value = value * 1.8d + 32d;
        }

        public void Convert(ref double value, out double normalized, out DataType targetType)
        {
            Convert(ref value);
            normalized = value;
            targetType = TargetType;
        }

        public DataType TargetType
        {
            get
            {
                return DataType.Fahrenheit;
            }
        }

        public bool IsDynamic
        {
            get
            {
                return false;
            }
        }

        private static CelciusToFahrenheit _instance { get; set; } = null;

        public static CelciusToFahrenheit Instance
        {
            get
            {
                if (_instance == null)
                {
                    _instance = new CelciusToFahrenheit();
                }

                return _instance;
            }
        }
    }

    public class MHzToGHz : iConverter
    {
        private MHzToGHz() { }

        public void Convert(ref double value)
        {
            value = value / 1000d;
        }

        public void Convert(ref double value, out double normalized, out DataType targetType)
        {
            Convert(ref value);
            normalized = value;
            targetType = TargetType;
        }

        public DataType TargetType
        {
            get
            {
                return DataType.GHz;
            }
        }

        public bool IsDynamic
        {
            get
            {
                return false;
            }
        }

        private static MHzToGHz _instance { get; set; } = null;

        public static MHzToGHz Instance
        {
            get
            {
                if (_instance == null)
                {
                    _instance = new MHzToGHz();
                }

                return _instance;
            }
        }
    }

    public class BitsPerSecondConverter : iConverter
    {
        private BitsPerSecondConverter() { }

        public void Convert(ref double value)
        {
            double _normalized;
            DataType _dataType;

            Convert(ref value, out _normalized, out _dataType);
        }

        public void Convert(ref double value, out double normalized, out DataType targetType)
        {
            normalized = value /= 128d;

            if (value < 1024d)
            {
                targetType = DataType.kbps;
                return;
            }
            else if (value < 1048576d)
            {
                value /= 1024d;
                targetType = DataType.Mbps;
                return;
            }
            else
            {
                value /= 1048576d;
                targetType = DataType.Gbps;
                return;
            }
        }

        public DataType TargetType
        {
            get
            {
                return DataType.kbps;
            }
        }

        public bool IsDynamic
        {
            get
            {
                return true;
            }
        }

        private static BitsPerSecondConverter _instance { get; set; } = null;

        public static BitsPerSecondConverter Instance
        {
            get
            {
                if (_instance == null)
                {
                    _instance = new BitsPerSecondConverter();
                }

                return _instance;
            }
        }
    }

    public class BytesPerSecondConverter : iConverter
    {
        private BytesPerSecondConverter() { }

        public void Convert(ref double value)
        {
            double _normalized;
            DataType _dataType;

            Convert(ref value, out _normalized, out _dataType);
        }

        public void Convert(ref double value, out double normalized, out DataType targetType)
        {
            normalized = value /= 1024d;

            if (value < 1024d)
            {
                targetType = DataType.kBps;
                return;
            }
            else if (value < 1048576d)
            {
                value /= 1024d;
                targetType = DataType.MBps;
                return;
            }
            else
            {
                value /= 1048576d;
                targetType = DataType.GBps;
                return;
            }
        }

        public DataType TargetType
        {
            get
            {
                return DataType.kBps;
            }
        }

        public bool IsDynamic
        {
            get
            {
                return true;
            }
        }

        private static BytesPerSecondConverter _instance { get; set; } = null;

        public static BytesPerSecondConverter Instance
        {
            get
            {
                if (_instance == null)
                {
                    _instance = new BytesPerSecondConverter();
                }

                return _instance;
            }
        }
    }

    public static class Extensions
    {
        public static bool IsEnabled(this MetricConfig[] metrics, MetricKey key)
        {
            return metrics.Any(m => m.Key == key && m.Enabled);
        }

        public static HardwareType[] GetHardwareTypes(this MonitorType type)
        {
            switch (type)
            {
                case MonitorType.CPU:
                    return new HardwareType[1] { HardwareType.Cpu };

                case MonitorType.RAM:
                    return new HardwareType[1] { HardwareType.Memory };

                case MonitorType.GPU:
                    return new HardwareType[2] { HardwareType.GpuNvidia, HardwareType.GpuAmd };

                default:
                    throw new ArgumentException("Invalid MonitorType.");
            }
        }

        public static string GetDescription(this MonitorType type)
        {
            switch (type)
            {
                case MonitorType.CPU:
                    return Resources.CPU;

                case MonitorType.RAM:
                    return Resources.RAM;

                case MonitorType.GPU:
                    return Resources.GPU;

                case MonitorType.HD:
                    return Resources.Drives;

                case MonitorType.Network:
                    return Resources.Network;

                default:
                    throw new ArgumentException("Invalid MonitorType.");
            }
        }

        public static T GetValue<T>(this ConfigParam[] parameters, ParamKey key)
        {
            return (T)parameters.Single(p => p.Key == key).Value;
        }

        public static string GetFullName(this MetricKey key)
        {
            switch (key)
            {
                case MetricKey.CPUClock:
                    return Resources.CPUClock;

                case MetricKey.CPUTemp:
                    return Resources.CPUTemp;

                case MetricKey.CPUVoltage:
                    return Resources.CPUVoltage;

                case MetricKey.CPUFan:
                    return Resources.CPUFan;

                case MetricKey.CPULoad:
                    return Resources.CPULoad;

                case MetricKey.CPUCoreLoad:
                    return Resources.CPUCoreLoad;

                case MetricKey.RAMClock:
                    return Resources.RAMClock;

                case MetricKey.RAMVoltage:
                    return Resources.RAMVoltage;

                case MetricKey.RAMLoad:
                    return Resources.RAMLoad;

                case MetricKey.RAMUsed:
                    return Resources.RAMUsed;

                case MetricKey.RAMFree:
                    return Resources.RAMFree;

                case MetricKey.GPUCoreClock:
                    return Resources.GPUCoreClock;

                case MetricKey.GPUVRAMClock:
                    return Resources.GPUVRAMClock;

                case MetricKey.GPUCoreLoad:
                    return Resources.GPUCoreLoad;

                case MetricKey.GPUVRAMLoad:
                    return Resources.GPUVRAMLoad;

                case MetricKey.GPUVoltage:
                    return Resources.GPUVoltage;

                case MetricKey.GPUTemp:
                    return Resources.GPUTemp;

                case MetricKey.GPUFan:
                    return Resources.GPUFan;

                case MetricKey.NetworkIP:
                    return Resources.NetworkIP;

                case MetricKey.NetworkExtIP:
                    return Resources.NetworkExtIP;

                case MetricKey.NetworkIn:
                    return Resources.NetworkIn;

                case MetricKey.NetworkOut:
                    return Resources.NetworkOut;

                case MetricKey.DriveLoadBar:
                    return Resources.DriveLoadBar;

                case MetricKey.DriveLoad:
                    return Resources.DriveLoad;

                case MetricKey.DriveUsed:
                    return Resources.DriveUsed;

                case MetricKey.DriveFree:
                    return Resources.DriveFree;

                case MetricKey.DriveRead:
                    return Resources.DriveRead;

                case MetricKey.DriveWrite:
                    return Resources.DriveWrite;

                default:
                    return "Unknown";
            }
        }

        public static string GetLabel(this MetricKey key)
        {
            switch (key)
            {
                case MetricKey.CPUClock:
                    return Resources.CPUClockLabel;

                case MetricKey.CPUTemp:
                    return Resources.CPUTempLabel;

                case MetricKey.CPUVoltage:
                    return Resources.CPUVoltageLabel;

                case MetricKey.CPUFan:
                    return Resources.CPUFanLabel;

                case MetricKey.CPULoad:
                    return Resources.CPULoadLabel;

                case MetricKey.CPUCoreLoad:
                    return Resources.CPUCoreLoadLabel;

                case MetricKey.RAMClock:
                    return Resources.RAMClockLabel;

                case MetricKey.RAMVoltage:
                    return Resources.RAMVoltageLabel;

                case MetricKey.RAMLoad:
                    return Resources.RAMLoadLabel;

                case MetricKey.RAMUsed:
                    return Resources.RAMUsedLabel;

                case MetricKey.RAMFree:
                    return Resources.RAMFreeLabel;

                case MetricKey.GPUCoreClock:
                    return Resources.GPUCoreClockLabel;

                case MetricKey.GPUVRAMClock:
                    return Resources.GPUVRAMClockLabel;

                case MetricKey.GPUCoreLoad:
                    return Resources.GPUCoreLoadLabel;

                case MetricKey.GPUVRAMLoad:
                    return Resources.GPUVRAMLoadLabel;

                case MetricKey.GPUVoltage:
                    return Resources.GPUVoltageLabel;

                case MetricKey.GPUTemp:
                    return Resources.GPUTempLabel;

                case MetricKey.GPUFan:
                    return Resources.GPUFanLabel;

                case MetricKey.NetworkIP:
                    return Resources.NetworkIPLabel;

                case MetricKey.NetworkExtIP:
                    return Resources.NetworkExtIPLabel;

                case MetricKey.NetworkIn:
                    return Resources.NetworkInLabel;

                case MetricKey.NetworkOut:
                    return Resources.NetworkOutLabel;

                case MetricKey.DriveLoadBar:
                    return Resources.DriveLoadBarLabel;

                case MetricKey.DriveLoad:
                    return Resources.DriveLoadLabel;

                case MetricKey.DriveUsed:
                    return Resources.DriveUsedLabel;

                case MetricKey.DriveFree:
                    return Resources.DriveFreeLabel;

                case MetricKey.DriveRead:
                    return Resources.DriveReadLabel;

                case MetricKey.DriveWrite:
                    return Resources.DriveWriteLabel;

                default:
                    return "Unknown";
            }
        }

        public static string GetAppend(this DataType type)
        {
            switch (type)
            {
                case DataType.Bit:
                    return " b";

                case DataType.Kilobit:
                    return " kb";

                case DataType.Megabit:
                    return " mb";

                case DataType.Gigabit:
                    return " gb";

                case DataType.Byte:
                    return " B";

                case DataType.Kilobyte:
                    return " KB";

                case DataType.Megabyte:
                    return " MB";

                case DataType.Gigabyte:
                    return " GB";

                case DataType.bps:
                    return " bps";

                case DataType.kbps:
                    return " kbps";

                case DataType.Mbps:
                    return " Mbps";

                case DataType.Gbps:
                    return " Gbps";

                case DataType.Bps:
                    return " B/s";

                case DataType.kBps:
                    return " kB/s";

                case DataType.MBps:
                    return " MB/s";

                case DataType.GBps:
                    return " GB/s";

                case DataType.MHz:
                    return " MHz";

                case DataType.GHz:
                    return " GHz";

                case DataType.Voltage:
                    return " V";

                case DataType.Percent:
                    return "%";

                case DataType.RPM:
                    return " RPM";

                case DataType.Celcius:
                    return " C";

                case DataType.Fahrenheit:
                    return " F";

                case DataType.IP:
                    return string.Empty;

                default:
                    throw new ArgumentException("Invalid DataType.");
            }
        }

        public static double Round(this double value, bool doRound)
        {
            if (!doRound)
            {
                return value;
            }

            return Math.Round(value);
        }
    }
}