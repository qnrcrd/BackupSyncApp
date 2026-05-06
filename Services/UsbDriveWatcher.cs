using System;
using System.Collections.Generic;
using System.Text;
using System.Windows.Threading;
using System.Threading.Tasks;
using System.Management;
using System.Runtime.InteropServices;
using Microsoft.Win32.SafeHandles;
using System.IO;

namespace BackupSyncApp.Services
{
    public class UsbDriveWatcher: IDisposable
    {
        public event Action<string> DriveConnected;
        public event Action<string> DriveDisconnected;

        private ManagementEventWatcher _watcher;
        private bool _isWatching=false;

        public void StartWatching()
        {
            if (_isWatching) return;

            try
            {
                WqlEventQuery query = new WqlEventQuery(
                    "SELECT * FROM Win32_VolumeChangeEvent WHERE EventType = 2 OR EventType = 3");

                _watcher = new ManagementEventWatcher(query);
                _watcher.EventArrived += OnDriveEvent;
                _watcher.Start();

                _isWatching = true;
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException($"could not start USB watching: {ex.Message}");
            }
        }

        public void StopWatching()
        {
            if (!_isWatching) return;

            _watcher?.Stop();
            _watcher?.Dispose();
            _watcher = null;
            _isWatching= false;
        }

        private void OnDriveEvent(object sender, EventArrivedEventArgs e)
        {
            try
            {
                int eventType = Convert.ToInt32(e.NewEvent["EventType"]);
                string driveName = e.NewEvent["DriveName"]?.ToString();

                if (string.IsNullOrEmpty(driveName)) return;

                // EVENT TYPE: 2 - CONNECTED, 3 - DISCONNECTED
                if (eventType == 2) DriveConnected?.Invoke(driveName);
                else if (eventType == 3) DriveDisconnected?.Invoke(driveName);
            }
            catch (Exception ex)
            {
                // error will be logged but process is NOT aborted
                Console.WriteLine($"disk event error: {ex.Message}");
            }
        }

        public static string GetDriveUniqueID(string drivePath)
        {
            try
            {
                var drive = new System.IO.DriveInfo(drivePath);
                if (drive.IsReady)
                {
                    ///
                    uint serialNumber = GetVolumeSerialNumber(drivePath);

                    return $"{drive.DriveType}_{serialNumber:X8}_{drive.TotalSize}";
                }
            }
            catch
            {
                // currently ignoring errors
            }
            return null;
        }

        // WINDOWS API ( VOLUME SERIAL ID )
        [DllImport("kernel32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        private static extern bool GetVolumeInformation(
            string rootPathName,
            StringBuilder volumeNameBuffer,
            int volumeNameSize,
            out uint volumeSerialNumber,
            out uint maximumComponentLength,
            out uint fileSystemFlags,
            StringBuilder fileSystemNameBuffer,
            int fileSystemNameSize);

        // WRAP UP
        private static uint GetVolumeSerialNumber(string drivePath)
        {
            try
            {
                StringBuilder volumeName = new StringBuilder(256);
                StringBuilder fileSystemName = new StringBuilder(256);
                uint serialNumber, maxComponentLen, fileSystemFlags;

                bool success = GetVolumeInformation(
                    drivePath.TrimEnd('\\') + "\\",
                    volumeName,
                    volumeName.Capacity,
                    out serialNumber,
                    out maxComponentLen,
                    out fileSystemFlags,
                    fileSystemName,
                    fileSystemName.Capacity);

                return success ? serialNumber : 0;
            }
            catch
            {
                return 0;
            }
        }

        public static string GetDriveLabel(string drivePath)
        {
            try
            {
                var drive = new System.IO.DriveInfo(drivePath);
                if (drive.IsReady)
                {

                    if (!string.IsNullOrEmpty(drive.VolumeLabel) && !string.IsNullOrWhiteSpace(drive.VolumeLabel) &&
                        drive.VolumeLabel!=drive.DriveType.ToString()) return drive.VolumeLabel;

                }

                return GetDriveTypeDescription(drive.DriveType);
            }
            catch { }
            
            return "Unknown drive";
            
        }

        private static string GetDriveTypeDescription(System.IO.DriveType driveType)
        {
            return driveType switch
            {
                DriveType.Removable => "USB Drive",
                DriveType.Fixed => "Hard Drive",
                DriveType.CDRom => "CD/DVD Drive",
                DriveType.Ram => "RAM Disk",
                DriveType.Network => "Network Drive",
                _ => "Unknown Type Drive"
            };
        }        

        public void Dispose()
        {
            StopWatching();
        }
    }
}
