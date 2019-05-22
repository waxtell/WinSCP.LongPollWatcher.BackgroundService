using System;

namespace WinSCP.LongPollWatcher.BackgroundService.Models.Internal
{
    internal class TrackedFile
    {
        public string FullName { get; }
        public long SizeInBytes { get; }
        public DateTime LastModified { get; }

        public TrackedFile(string fullName, long sizeInBytes, DateTime lastModified)
        {
            FullName = fullName;
            SizeInBytes = sizeInBytes;
            LastModified = lastModified;
        }

        public override bool Equals(object other)
        {
            if (other is TrackedFile o)
            {
                return FullName.Equals(o.FullName);
            }

            return false;
        }

        public override int GetHashCode()
        {
            return FullName.GetHashCode();
        }
    }
}