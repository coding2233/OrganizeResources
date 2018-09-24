using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using System.IO;
using System.Text;
using System.Threading.Tasks;
//using System;

public class OrganizeResourcesEditor : EditorWindow {
    
    private Vector2 _scrollVec2;

    private List<string> _allAssetsPaths = new List<string>();
    private List<KeyValuePair<string,bool>> _unusedAssets = new List<KeyValuePair<string, bool>>();
    private List<List<string>> _repeatAssetsPaths = new List<List<string>>();

    private List<List<bool>> _RepeatSelect = new List<List<bool>>();

    enum ControllerState
    {
        NONE,
        ALL,
        UNUSED,
        REPEAT,
    }
    ControllerState CtrlState = ControllerState.NONE;

    private int _dataMaxRow = 20;
    private int _dataIndex=8;

    [MenuItem("Organize Resources/Editor Window")]
    static void main()
    {
        OrganizeResourcesEditor organizeResourcesEditor = EditorWindow.GetWindowWithRect<OrganizeResourcesEditor>(new Rect(0, 0, 800, 600), true, "Resources", true);
        // Debug.Log(Application.dataPath);
    }


    //
    #region GUI
    void OnGUI()
    {
        _scrollVec2 = GUILayout.BeginScrollView(_scrollVec2);
        GUILayout.Label("删除功能请慎重操作");
        #region 三个按钮

        GUIStyle _AllBtnStyle = GUI.skin.GetStyle("flow node 2");
        GUIStyle _UnusedBtnStyle = GUI.skin.GetStyle("flow node 2");
        GUIStyle _RepeatBtnStyle = GUI.skin.GetStyle("flow node 2");
        
        switch (CtrlState)
        {
            case ControllerState.ALL:
                _AllBtnStyle = GUI.skin.GetStyle("flow node 2 on");
                _UnusedBtnStyle = GUI.skin.GetStyle("flow node 2");
                _RepeatBtnStyle = GUI.skin.GetStyle("flow node 2");
                break;
            case ControllerState.UNUSED:
                _AllBtnStyle = GUI.skin.GetStyle("flow node 2");
                _UnusedBtnStyle = GUI.skin.GetStyle("flow node 2 on");
                _RepeatBtnStyle = GUI.skin.GetStyle("flow node 2");
                break;
            case ControllerState.REPEAT:
                _AllBtnStyle = GUI.skin.GetStyle("flow node 2");
                _UnusedBtnStyle = GUI.skin.GetStyle("flow node 2");
                _RepeatBtnStyle = GUI.skin.GetStyle("flow node 2 on");
                break;

            default:
                break;
        }

        GUILayout.Space(10);
        EditorGUILayout.BeginHorizontal();
        GUILayout.FlexibleSpace();
        if (GUILayout.Button("未使用资源", _UnusedBtnStyle, new GUILayoutOption[] { GUILayout.Height(30), GUILayout.Width(200) }))
        {
            OnUnusedClick();
            CtrlState = ControllerState.UNUSED;
     
        }
        GUILayout.FlexibleSpace();

        if (GUILayout.Button("全部资源", _AllBtnStyle, new GUILayoutOption[] { GUILayout.Height(30), GUILayout.Width(200) }))
        {
            OnAllClick();
            CtrlState = ControllerState.ALL;
        }
        GUILayout.FlexibleSpace();

        if (GUILayout.Button("重复资源", _RepeatBtnStyle, new GUILayoutOption[] { GUILayout.Height(30), GUILayout.Width(200) }))//
        {
            OnRepeatClick();
            CtrlState = ControllerState.REPEAT;
        }
        GUILayout.FlexibleSpace();
        EditorGUILayout.EndHorizontal();
        GUILayout.Space(5);
        #endregion

        int dataCount=0;
        switch (CtrlState)
        {
            case ControllerState.ALL:
                OnAllGUI();
                dataCount = _allAssetsPaths.Count;
                break;
            case ControllerState.UNUSED:
                OnUnsedGUI();
                dataCount = _unusedAssets.Count;
                break;
            case ControllerState.REPEAT:
                OnRepeatGUI();
                break;

            default:
                break;
        }
      
        GUILayout.EndScrollView();

        //更新数据
        if (Mathf.Approximately(_scrollVec2.y, 0.0f))
            DataScrollViewUpdate(-1, dataCount);
        else if ((_scrollVec2.y>=99.0))
            DataScrollViewUpdate(1, dataCount);
    }
    #endregion

    #region 三个分类按钮
    async void OnAllClick()
    {
        #region 获取所有的asset
        _allAssetsPaths = await UpdateAllFilePaths();

        //_AllAssetsPaths = new List<string>();
        //string[] _tempList = AssetDatabase.GetAllAssetPaths();
        //for (int i = 0; i < _tempList.Length; i++)
        //{
        //    if (_tempList[i].Contains("Assets"))
        //    {
        //        if (!AssetDatabase.IsValidFolder(_tempList[i]))
        //        {
        //            //屏蔽一些特殊文件夹
        //            bool _isUnused = _tempList[i].Contains("Editor") || _tempList[i].Contains("Editor Default Resources")
        //                || _tempList[i].Contains("Gizmos") || _tempList[i].Contains("Plugins")
        //                || _tempList[i].Contains("Resources") || _tempList[i].Contains("StreamingAssets");
        //            if (!_isUnused)
        //            {
        //                _AllAssetsPaths.Add(_tempList[i]);
        //            }
        //        }

        //    }
        //}
        #endregion
    }

    async void OnUnusedClick()
    {
        _allAssetsPaths = await UpdateAllFilePaths();

        _unusedAssets = new List<KeyValuePair<string, bool>>();
        for (int i = 0; i < _allAssetsPaths.Count; i++)
        {
            if (GetUseAssetPaths(_allAssetsPaths[i]).Length <= 0)
            {
                _unusedAssets.Add(new KeyValuePair<string, bool>(_allAssetsPaths[i],false));
            }
            EditorUtility.DisplayProgressBar("整理未使用资源中", _allAssetsPaths[i], (float)i / (float)_allAssetsPaths.Count);
        }
        EditorUtility.ClearProgressBar();
    }
    void OnRepeatClick()
    {
        RefreshForceText();
        OnAllClick();
        
         _repeatAssetsPaths = new List<List<string>>();
        _RepeatSelect = new List<List<bool>>();
        List<string> _TempListRepeat = new List<string>();
        for (int i = 0; i < _allAssetsPaths.Count; i++)
        {
            if (GetSameFilePaths(_allAssetsPaths[i]).Length > 0)
                _TempListRepeat.Add(_allAssetsPaths[i]);
            EditorUtility.DisplayProgressBar("查找重复资源中", _allAssetsPaths[i], (float)i / (float)_allAssetsPaths.Count);
        }

        //  List<List<string>> _List = new List<List<string>>();
        for (int i = 0; i < _TempListRepeat.Count; i++)
        {
            EditorUtility.DisplayProgressBar("整理重复资源中", _TempListRepeat[i], (float)i+1.0f / (float)_TempListRepeat.Count);
            List<string> _TempMake = new List<string>();
            List<bool> _TempSelect = new List<bool>();
            _TempMake.Add(_TempListRepeat[i]);
            _TempSelect.Add(false);
            for (int j = 0; j < _TempListRepeat.Count; j++)
            {
                if (i == j)
                    continue;
                if (GetFileMD5(_TempListRepeat[i]) == GetFileMD5(_TempListRepeat[j]))
                {
                    _TempMake.Add(_TempListRepeat[j]);
                    _TempSelect.Add(false);
                    _TempListRepeat.RemoveAt(j);
                    j--;
                }
            }
            _repeatAssetsPaths.Add(_TempMake);
            _RepeatSelect.Add(_TempSelect);
            _TempListRepeat.RemoveAt(i);
            i--;
        }
        EditorUtility.ClearProgressBar();
    }
    #endregion

    #region 所有资源GUI
    void OnAllGUI()
    {
        if (_allAssetsPaths == null || _allAssetsPaths.Count == 0)
        {
            GUILayout.Label("资源加载中...");
            return;
        }

        if (_allAssetsPaths.Count > 0)
        {
            EditorGUILayout.BeginHorizontal("HelpBox");
            GUILayout.Label("图标", new GUILayoutOption[] { GUILayout.Width(40), GUILayout.Height(20) });
            GUILayout.Label("名称", GUILayout.Width(300));
            GUILayout.FlexibleSpace();
            GUILayout.Label("", GUILayout.Width(100));
            GUILayout.FlexibleSpace();
            GUILayout.Label("", GUILayout.Width(100));
            GUILayout.FlexibleSpace();
            EditorGUILayout.EndHorizontal();
        }

        int rowCount = _allAssetsPaths.Count <= _dataMaxRow ? _allAssetsPaths.Count : _dataMaxRow;
        rowCount += _dataIndex;
        for (int i = _dataIndex; i < rowCount; i++)
        {
            EditorGUILayout.BeginHorizontal("HelpBox");//
            GUILayout.Label(AssetDatabase.GetCachedIcon(_allAssetsPaths[i]), new GUILayoutOption[] { GUILayout.Width(30), GUILayout.Height(20) });
            GUILayout.Label(AssetDatabase.LoadMainAssetAtPath(_allAssetsPaths[i]).name, GUILayout.Width(300));
            GUILayout.FlexibleSpace();
            if (GUILayout.Button("InstanceID", GUILayout.Width(80)))
            {
                Debug.Log(AssetDatabase.LoadAssetAtPath<Object>(_allAssetsPaths[i]).GetInstanceID());
            }
            GUILayout.FlexibleSpace();
            if (GUILayout.Button("GUID", GUILayout.Width(80)))
            {
                Debug.Log(AssetDatabase.AssetPathToGUID(_allAssetsPaths[i]));
            }
            GUILayout.FlexibleSpace();
            if (GUILayout.Button("引用", GUILayout.Width(80)))
            {
                string[] _TempArray = AssetDatabase.GetDependencies(_allAssetsPaths[i]);
                string _Temp = "";
                for (int m = 0; m < _TempArray.Length; m++)
                {
                    _Temp += _TempArray[m]+"\n";
                }
                Debug.Log(_Temp);
            }
            GUILayout.FlexibleSpace();
            if (GUILayout.Button("被引用", GUILayout.Width(80)))
            {
                string[] _TempArray = GetUseAssetPaths(_allAssetsPaths[i]);
                string _Temp = "";
                for (int m = 0; m < _TempArray.Length; m++)
                {
                    _Temp += _TempArray[m] + "\n";
                }
                Debug.Log(_Temp);
            }
            GUILayout.FlexibleSpace();
            EditorGUILayout.EndHorizontal();
        }
    }
    #endregion

    #region 未使用资源GUI
    void OnUnsedGUI()
    {
        if (_unusedAssets == null || _unusedAssets.Count == 0)
        {
            GUILayout.Label("资源加载中...");
            return;
        }

        if (_unusedAssets.Count > 0)
        {
            EditorGUILayout.BeginHorizontal("HelpBox");
            GUILayout.Label("图标", new GUILayoutOption[] { GUILayout.Width(40), GUILayout.Height(20) });
            GUILayout.Label("名称", GUILayout.Width(300));
            GUILayout.FlexibleSpace();
            GUILayout.Label("", GUILayout.Width(100));
            GUILayout.FlexibleSpace();
            if (GUILayout.Button("删除所选", GUILayout.Width(100)))
            {
                OnUnusedDelete();
            }
            GUILayout.FlexibleSpace();
            EditorGUILayout.EndHorizontal();

        }

        int rowCount = _unusedAssets.Count <= _dataMaxRow ? _unusedAssets.Count : _dataMaxRow;
        rowCount += _dataIndex;
        
        for (int i = _dataIndex; i < rowCount; i++)
        {
            EditorGUILayout.BeginHorizontal("HelpBox");
            GUILayout.Label(AssetDatabase.GetCachedIcon(_unusedAssets[i].Key), new GUILayoutOption[] { GUILayout.Width(40), GUILayout.Height(20) });
            GUILayout.Label(AssetDatabase.LoadMainAssetAtPath(_unusedAssets[i].Key).name, GUILayout.Width(300));
            GUILayout.FlexibleSpace();
            if (GUILayout.Button("选中", GUILayout.Width(100)))
            {
                OnUnusedSelect(_unusedAssets[i].Key);
            }
            GUILayout.FlexibleSpace();
            GUILayout.Space(100);
            //因为删除一次就会更新一次资源  所以提供选项 一次性删除
            bool value= GUILayout.Toggle(_unusedAssets[i].Value, "删除", GUILayout.Width(100));
            if(_unusedAssets[i].Value!= value)
                _unusedAssets[i] = new KeyValuePair<string, bool>(_unusedAssets[i].Key,value);
            GUILayout.FlexibleSpace();
            EditorGUILayout.EndHorizontal();
        }
    }
    void OnUnusedSelect(string _PathValue)
    {
        Selection.activeObject = AssetDatabase.LoadAssetAtPath<Object>(_PathValue);
    }
    void OnUnusedDelete()
    {
        bool _isDelete = false;
        if (_unusedAssets.Count == _unusedAssets.Count)
        {
            for (int i = 0; i < _unusedAssets.Count; i++)
            {
                if (_unusedAssets[i].Value)
                {
                    AssetDatabase.DeleteAsset(_unusedAssets[i].Key);
                    _isDelete = true;
                }
            }
        }
        if (_isDelete)
        {
            AssetDatabase.Refresh();
            OnUnusedClick();
        }
    }
    #endregion

    #region 重复资源GUI
    void OnRepeatGUI()
    {
        if (_repeatAssetsPaths.Count > 0)
        {
            EditorGUILayout.BeginHorizontal("HelpBox");
            GUILayout.Label("图标", new GUILayoutOption[] { GUILayout.Width(40), GUILayout.Height(20) });
            GUILayout.Label("名称", GUILayout.Width(300));
            GUILayout.FlexibleSpace();
            GUILayout.Label("", GUILayout.Width(100));
            GUILayout.FlexibleSpace();
            if (GUILayout.Button("合并所选", GUILayout.Width(100)))
            {
                OnQueryRepeatMerge();
            }
            GUILayout.FlexibleSpace();
            EditorGUILayout.EndHorizontal();
        }

        for (int i = 0; i < _repeatAssetsPaths.Count; i++)
        {
            EditorGUILayout.BeginVertical("HelpBox");
            for (int j = 0; j < _repeatAssetsPaths[i].Count; j++)
            {
                EditorGUILayout.BeginHorizontal("HelpBox");
                GUILayout.Label(AssetDatabase.GetCachedIcon(_repeatAssetsPaths[i][j]), new GUILayoutOption[] { GUILayout.Width(40), GUILayout.Height(20) });
                GUILayout.Label(AssetDatabase.LoadMainAssetAtPath(_repeatAssetsPaths[i][j]).name, GUILayout.Width(300));
                GUILayout.FlexibleSpace();
                if (GUILayout.Button("选中", GUILayout.Width(100)))
                {
                    OnUnusedSelect(_repeatAssetsPaths[i][j]);
                }
                GUILayout.FlexibleSpace();
                GUILayout.Space(100);
                _RepeatSelect[i][j] = GUILayout.Toggle(_RepeatSelect[i][j], "合并", GUILayout.Width(100));
                for (int m = 0; m < _RepeatSelect[i].Count; m++)
                {
                    if (m != j&& _RepeatSelect[i][j])
                    {
                        _RepeatSelect[i][m] = false;
                    }
                }
                //if (GUILayout.Button("合并", GUILayout.Width(100)))
                //{
                //    OnRepeatMerge(_RepeatAssetsPaths[i][j], _RepeatAssetsPaths[i]);
                //    break;
                //}
                GUILayout.FlexibleSpace();
                EditorGUILayout.EndHorizontal();
            }
            EditorGUILayout.EndVertical();
        }
    }

    #region 合并
    void OnQueryRepeatMerge()
    {
        bool _isMerge = false;
        if (_RepeatSelect.Count != _repeatAssetsPaths.Count)
            return;
        for (int i = 0; i < _repeatAssetsPaths.Count; i++)
        {
            if (_RepeatSelect[i].Count != _repeatAssetsPaths[i].Count)
                break;
            for (int j = 0; j < _repeatAssetsPaths[i].Count; j++)
            {
                if (_RepeatSelect[i][j])
                {
                    OnRepeatMerge(_repeatAssetsPaths[i][j], _repeatAssetsPaths[i]);
                    _isMerge = true;
                }
            }
        }
        if (_isMerge)
        {
            AssetDatabase.Refresh();
            OnRepeatClick();
        }
    }

    private void OnRepeatMerge(string _PathValue, List<string> _ListValue)
    {
        string _FixedGUID= AssetDatabase.AssetPathToGUID(_PathValue);
        string _AssetsPath = Application.dataPath.Replace("Assets", "");

        for (int i = 0; i < _ListValue.Count; i++)
        {
            if (_PathValue == _ListValue[i])
                continue;
            string[] _OtherPaths = GetUseAssetPaths(_ListValue[i]);
            
            bool _isOtherNative = true;
            string _OldGUI = AssetDatabase.AssetPathToGUID(_ListValue[i]);
            for (int j = 0; j < _OtherPaths.Length; j++)
            {
                Object _OtherUseAsset = AssetDatabase.LoadAssetAtPath<Object>(_OtherPaths[j]);
                if (AssetDatabase.IsNativeAsset(_OtherUseAsset))
                {
                    string _RealAllText = File.ReadAllText(_AssetsPath + _OtherPaths[j]).Replace(_OldGUI, _FixedGUID);
                    File.WriteAllText(_AssetsPath + _OtherPaths[j], _RealAllText);
                }
                else
                {
                    //外部引用  --   外部资源引用到材质好像就模型了吧   一时也想不到其他东西
                    if (_PathValue.EndsWith(".mat"))
                    {
                        Object _ChangeObejct = AssetDatabase.LoadAssetAtPath<Object>(_ListValue[i]);
                        GameObject _MeshObject = AssetDatabase.LoadAssetAtPath<GameObject>(_OtherPaths[j]);
                        MeshRenderer[] _KidsMR = _MeshObject.GetComponentsInChildren<MeshRenderer>();
                        for (int m = 0; m < _KidsMR.Length; m++)
                        {
                            Material[] _TempShared = _KidsMR[m].sharedMaterials;
                            for (int n = 0; n < _TempShared.Length; n++)
                            {
                                if (_ChangeObejct.GetInstanceID() == _TempShared[n].GetInstanceID())
                                {
                                    _TempShared[n] = AssetDatabase.LoadAssetAtPath<Material>(_PathValue);
                                }
                                _KidsMR[m].materials = _TempShared;
                            }
                        }
                    }
                    else
                        _isOtherNative = false;
                }
            }
            //如果没有外部资源引用他 就删除
            if (_isOtherNative)
            {
                AssetDatabase.DeleteAsset(_ListValue[i]);
                _ListValue.RemoveAt(i);
                i--;
            }
        }
    }

    #endregion
    #endregion

    #region 获取其他引用Assets的路径
    private string[] GetUseAssetPaths(string assetPath)
    {
        List<string> assetPaths = new List<string>();
        //使用GUID作为判断标准
        string assetGUID = AssetDatabase.AssetPathToGUID(assetPath);
        //遍历所有Assets
        for (int i = 0; i < _allAssetsPaths.Count; i++)
        {
            if (_allAssetsPaths[i].Equals(assetPath))
                continue;

            string[] otherPaths = AssetDatabase.GetDependencies(_allAssetsPaths[i]);
            if (otherPaths.Length > 1)
            {
                for (int j = 0; j < otherPaths.Length; j++)
                {
                    string _OtherGUID = AssetDatabase.AssetPathToGUID(otherPaths[j]);
                    if (assetGUID == _OtherGUID)
                    {
                        assetPaths.Add(_allAssetsPaths[i]);
                    }
                }
            }
        }
        return assetPaths.ToArray();
    }
    #endregion

    #region 获取相同的文件
    string[] GetSameFilePaths(string _PathValue)
    {
        List<string> _AssetPaths = new List<string>();

        string _AssetMD5 = GetFileMD5(_PathValue);
        //遍历所有Assets
        for (int i = 0; i < _allAssetsPaths.Count; i++)
        {
            if (_allAssetsPaths[i] == _PathValue)
                continue;
                if (_AssetMD5 == GetFileMD5(_allAssetsPaths[i]))
                    _AssetPaths.Add(_allAssetsPaths[i]);

        }
        return _AssetPaths.ToArray();
    }
    #region 获取文件的MD5值
    string GetFileMD5(string _PathValue)
    {
        //判断是否为本地资源   因为本地文件里有文件名称 但是在资源名称又不能重复  于是需要去掉名称 来检测md5值
        Object _ObejctValue = AssetDatabase.LoadAssetAtPath<Object>(_PathValue);
        bool _isNative =AssetDatabase.IsNativeAsset(_ObejctValue);
        string _FileMD5 = "";
        string _TemPath = Application.dataPath.Replace("Assets", "");

        if (_isNative)
        {
            string _TempFileText = File.ReadAllText(_TemPath + _PathValue).Replace("m_Name: " + _ObejctValue.name,"");
        
            System.Security.Cryptography.MD5 md5 = new System.Security.Cryptography.MD5CryptoServiceProvider();
            //将字符串转换为字节数组  
            byte[] fromData = System.Text.Encoding.Unicode.GetBytes(_TempFileText);
            //计算字节数组的哈希值  
            byte[] toData = md5.ComputeHash(fromData);
            _FileMD5 = "";
            for (int i = 0; i < toData.Length; i++)
            {
                _FileMD5 += toData[i].ToString("x2");
            }
        }
        else
        {
            _FileMD5 = "";
            //外部文件的MD5值
            try
            {

                FileStream fs = new FileStream(_TemPath + _PathValue, FileMode.Open);

                System.Security.Cryptography.MD5 md5 = new System.Security.Cryptography.MD5CryptoServiceProvider();
                byte[] retVal = md5.ComputeHash(fs);
                fs.Close();
                for (int i = 0; i < retVal.Length; i++)
                {
                    _FileMD5 += retVal[i].ToString("x2");
                }
            }
            catch (System.Exception ex)
            {
                Debug.Log(ex);
            }
            //因为外部文件还存在不同的设置问题，还需要检测一下外部资源的.meta文件
            if (_FileMD5 != "")
            {
                string _MetaPath = AssetDatabase.GetTextMetaFilePathFromAssetPath(_PathValue);
                string _ObjectGUID = AssetDatabase.AssetPathToGUID(_PathValue);
                //去掉guid来检测
                string _TempFileText = File.ReadAllText(_TemPath + _MetaPath).Replace("guid: " + _ObjectGUID, "");

                System.Security.Cryptography.MD5 _MetaMd5 = new System.Security.Cryptography.MD5CryptoServiceProvider();
                //将字符串转换为字节数组  
                byte[] fromData = System.Text.Encoding.Unicode.GetBytes(_TempFileText);
                //计算字节数组的哈希值  
                byte[] toData = _MetaMd5.ComputeHash(fromData);
                for (int i = 0; i < toData.Length; i++)
                {
                    _FileMD5 += toData[i].ToString("x2");
                }
            }
        }
        return _FileMD5;
    }
    #endregion
    #endregion


    [MenuItem("Assets/Load File", false, 10)]
    static void DebugFileString()
    {
        RefreshForceText();

        if (!Selection.activeObject)
            return;

        Object _ActiveObject = Selection.activeObject;
        string _ObjectPath = AssetDatabase.GetAssetPath(_ActiveObject);
       // string _Path = Application.dataPath.Replace("Assets", "");
        //string _Context = File.ReadAllText(_Path + _ObjectPath);

      Debug.Log(AssetDatabase.GetTextMetaFilePathFromAssetPath(_ObjectPath));

        //Debug.Log(AssetDatabase.IsNativeAsset(_ActiveObject) + "  ####  " + _Context.Contains("m_Name: " + _ActiveObject.name) + "   @@@@  " + _Context);
        //_Context = "";
        //string[] _TempArray = File.ReadAllLines(_Path + _ObjectPath);
        //for (int i = 0; i < _TempArray.Length; i++)
        //{
        //    if (_TempArray[i].Contains("m_Name: "))
        //    {
        //        _TempArray[i] = "";
        //    }
        //    _Context += _TempArray[i];
        //    if (i != _TempArray.Length - 1)
        //        _Context += "\n";
        //}
    }

    #region 刷新Asset的Text
    static void RefreshForceText()
    {
        AssetDatabase.Refresh();
        //刷新文件的数据 -- 没找到其他接口
        // EditorSettings.serializationMode = SerializationMode.ForceBinary;
        // EditorSettings.serializationMode = SerializationMode.ForceText;
    }
    #endregion

    //数据更新
    private void DataScrollViewUpdate(int value,int count)
    {
        int maxIndex = Mathf.Max(0, count - _dataMaxRow);
        _dataIndex += value;
        if (_dataIndex < 0)
            _dataIndex = 0;
        else if (_dataIndex > maxIndex)
            _dataIndex = maxIndex;
      //  Debug.Log($"DataScrollViewUpdate:{value}");
    }

    //获取所有文件的路径
    private async Task<List<string>> UpdateAllFilePaths()
    {
        RefreshForceText();
        string[] folders = new string[] { "\\Editor", "\\Editor Default Resources", "\\Gizmos", "\\Plugins", "\\Resources", "\\StreamingAssets", "\\Packages" };
        string[] extension = new string[] { ".meta", ".cs" };
        return await AssetFileData.GetAllFilePaths(Application.dataPath, folders, extension);
    }
}
