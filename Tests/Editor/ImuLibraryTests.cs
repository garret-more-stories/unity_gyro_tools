#if UNITY_EDITOR
using NUnit.Framework;
using System.Runtime.InteropServices;

namespace MoreStories.GyroTools.Editor.Tests
{
    public class ImuLibraryTests
    {
#if UNITY_EDITOR_WIN
        const string imu_library = "imu_reader";
#else
        const string imu_library = "libimu_reader";
#endif
        const int TestNumber = 2;
        [DllImport(imu_library, CallingConvention = CallingConvention.Cdecl)]
        public static extern int return_number_two();
        [Test]
        public void CheckDllExport()
        {
            Assert.AreEqual(TestNumber, return_number_two(), 
            "Expected number from test method not received, DLL import failed.");
        }
    }
}
#endif