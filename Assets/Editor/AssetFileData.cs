using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;
using System.IO;

public class AssetFileData  {

    public static async Task<List<string>> GetAllFilePaths(string folder,string[] ignoreFolders,string[] ignoreExtension)
    {
        folder = Path.GetFullPath(folder);

        List<string> paths = new List<string>();
        await Task.Run(() => 
            {
                List<FileInfo> fileInfos = GetAllFiles(folder);
                foreach (var item in fileInfos)
                {
                    //忽略文件夹
                    if (ignoreFolders != null)
                    {
                        bool result=false;
                        for (int i = 0; i < ignoreFolders.Length; i++)
                        {
                            if (item.FullName.Contains(ignoreFolders[i]))
                            {
                                result = true;
                                break; 
                            }
                        }
                        if (result)
                            continue;
                    }
                    //忽略文件
                    if (ignoreExtension != null)
                    {
                        bool result = false;
                        for (int i = 0; i < ignoreExtension.Length; i++)
                        {
                            if (item.Extension.Equals(ignoreExtension[i]))
                            {
                                result = true;
                                break;
                            }
                        }
                        if (result)
                            continue;
                    }
                    string path = $"Assets{item.FullName.Replace(folder,"")}";
                    //添加文件路径
                    paths.Add(path);
                }
            }
        );
        return paths;
    }

    /// <summary>
    /// 获取所有的文件
    /// </summary>
    /// <param name="dirtector"></param>
    /// <returns></returns>
    public static List<FileInfo> GetAllFiles(string folder)
    {
        List<FileInfo> allfiles = new List<FileInfo>();
        DirectoryInfo theFolder = new DirectoryInfo(folder);
        FileInfo[] fileInfos = theFolder.GetFiles();
        foreach (var item in fileInfos)
            allfiles.Add(item);
        DirectoryInfo[] directoryInfos = theFolder.GetDirectories();
        foreach (var item in directoryInfos)
            allfiles.AddRange(GetAllFiles(item.FullName));
        return allfiles;
    }
}
