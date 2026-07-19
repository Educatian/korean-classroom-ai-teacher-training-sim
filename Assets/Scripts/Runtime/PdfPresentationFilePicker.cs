using System;
using System.Runtime.InteropServices;

namespace AdieLab.TeacherTraining
{
    public static class PdfPresentationFilePicker
    {
        public static string PickPdf()
        {
#if UNITY_EDITOR
            return UnityEditor.EditorUtility.OpenFilePanel("전자칠판 PDF 불러오기", string.Empty, "pdf");
#elif UNITY_STANDALONE_WIN
            OpenFileName dialog = new OpenFileName
            {
                structSize = Marshal.SizeOf(typeof(OpenFileName)),
                filter = "PDF documents (*.pdf)\0*.pdf\0\0",
                file = new string(new char[32768]),
                maxFile = 32768,
                title = "전자칠판 PDF 불러오기",
                defExt = "pdf",
                flags = 0x00001000 | 0x00000800 | 0x00000008
            };
            return GetOpenFileName(dialog) ? dialog.file.TrimEnd('\0') : string.Empty;
#else
            return string.Empty;
#endif
        }

#if UNITY_STANDALONE_WIN && !UNITY_EDITOR
        [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
        private sealed class OpenFileName
        {
            public int structSize;
            public IntPtr dlgOwner = IntPtr.Zero;
            public IntPtr instance = IntPtr.Zero;
            public string filter;
            public string customFilter;
            public int maxCustFilter;
            public int filterIndex;
            public string file;
            public int maxFile;
            public string fileTitle;
            public int maxFileTitle;
            public string initialDir;
            public string title;
            public int flags;
            public short fileOffset;
            public short fileExtension;
            public string defExt;
            public IntPtr custData = IntPtr.Zero;
            public IntPtr hook = IntPtr.Zero;
            public string templateName;
            public IntPtr reservedPtr = IntPtr.Zero;
            public int reservedInt;
            public int flagsEx;
        }

        [DllImport("comdlg32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool GetOpenFileName([In, Out] OpenFileName dialog);
#endif
    }
}