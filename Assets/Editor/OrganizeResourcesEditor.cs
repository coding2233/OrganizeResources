using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using System.IO;
using System.Text;
//using System;

public class OrganizeResourcesEditor : EditorWindow {

    [MenuItem("Organize Resources/Editor Window")]
    static void main()
    {
        EditorWindow.GetWindow<OrganizeResourcesEditor>("Resources");
       // Debug.Log(Application.dataPath);
    }

    public Object imageObj;
    private Vector2 scrollVec2;

    private List<string> _AllAssetsPaths = new List<string>();
    private List<string> _UnusedAssetsPaths = new List<string>();
    private List<List<string>> _RepeatAssetsPaths = new List<List<string>>();

    enum ControllerState
    {
        NONE,
        ALL,
        UNUSED,
        REPEAT,
    }
    ControllerState CtrlState = ControllerState.NONE;
    
    //
    #region GUI
    void OnGUI()
    {
        scrollVec2 = GUILayout.BeginScrollView(scrollVec2);
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

        switch (CtrlState)
        {
            case ControllerState.ALL:
                OnAllGUI();
                break;
            case ControllerState.UNUSED:
                OnUnsedGUI();
                break;
            case ControllerState.REPEAT:
                OnRepeatGUI();
                break;

            default:
                break;
        }
      
        GUILayout.EndScrollView();
    }
    #endregion

    #region 三个分类按钮
    void OnAllClick()
    {
        RefreshForceText();
        #region 获取所有的asset
        _AllAssetsPaths = new List<string>();
        string[] _tempList = AssetDatabase.GetAllAssetPaths();
        for (int i = 0; i < _tempList.Length; i++)
        {
            if (_tempList[i].Contains("Assets"))
                if (!AssetDatabase.IsValidFolder(_tempList[i]))
                    _AllAssetsPaths.Add(_tempList[i]);
        }
        #endregion
    }
    void OnUnusedClick()
    {
        RefreshForceText();
        OnAllClick();

        _UnusedAssetsPaths = new List<string>();
        for (int i = 0; i < _AllAssetsPaths.Count; i++)
        {
            if (GetUseAssetPaths(_AllAssetsPaths[i]).Length <= 0)
            {
                //屏蔽一些特殊文件夹
                bool _isUnused = _AllAssetsPaths[i].Contains("Editor")|| _AllAssetsPaths[i].Contains("Editor Default Resources") 
                    || _AllAssetsPaths[i].Contains("Gizmos") ||  _AllAssetsPaths[i].Contains("Plugins") 
                    || _AllAssetsPaths[i].Contains("Resources") || _AllAssetsPaths[i].Contains("StreamingAssets");
                if (!_isUnused)
                {
                    _UnusedAssetsPaths.Add(_AllAssetsPaths[i]);
                }
            }
            EditorUtility.DisplayProgressBar("整理未使用资源中", _AllAssetsPaths[i], (float)i / (float)_AllAssetsPaths.Count);
        }
        EditorUtility.ClearProgressBar();

    }
    void OnRepeatClick()
    {
        RefreshForceText();
        OnAllClick();
        
         _RepeatAssetsPaths = new List<List<string>>();
        List<string> _TempListRepeat = new List<string>();
        for (int i = 0; i < _AllAssetsPaths.Count; i++)
        {
            if (GetSameFilePaths(_AllAssetsPaths[i]).Length > 0)
                _TempListRepeat.Add(_AllAssetsPaths[i]);
            EditorUtility.DisplayProgressBar("查找重复资源中", _AllAssetsPaths[i], (float)i / (float)_AllAssetsPaths.Count);
        }

        //  List<List<string>> _List = new List<List<string>>();
        for (int i = 0; i < _TempListRepeat.Count; i++)
        {
            EditorUtility.DisplayProgressBar("整理重复资源中", _TempListRepeat[i], (float)i+1.0f / (float)_TempListRepeat.Count);
            List<string> _TempMake = new List<string>();
            _TempMake.Add(_TempListRepeat[i]);
            for (int j = 0; j < _TempListRepeat.Count; j++)
            {
                if (i == j)
                    continue;
                if (GetFileMD5(_TempListRepeat[i]) == GetFileMD5(_TempListRepeat[j]))
                {
                    _TempMake.Add(_TempListRepeat[j]);
                    _TempListRepeat.RemoveAt(j);
                    j--;
                }
            }
            _RepeatAssetsPaths.Add(_TempMake);
            _TempListRepeat.RemoveAt(i);
            i--;
        }
        EditorUtility.ClearProgressBar();
    }
    #endregion

    #region 所有资源GUI
    void OnAllGUI()
    {
        for (int i = 0; i < _AllAssetsPaths.Count; i++)
        {
            EditorGUILayout.BeginHorizontal("HelpBox");//
            GUILayout.Label(AssetDatabase.GetCachedIcon(_AllAssetsPaths[i]), new GUILayoutOption[] { GUILayout.Width(30), GUILayout.Height(20) });
            GUILayout.Label(AssetDatabase.LoadMainAssetAtPath(_AllAssetsPaths[i]).name, GUILayout.Width(300));
            GUILayout.FlexibleSpace();
            if (GUILayout.Button("GUID", GUILayout.Width(100)))
            {
                Debug.Log(AssetDatabase.AssetPathToGUID(_AllAssetsPaths[i]));
            }
            GUILayout.FlexibleSpace();
            if (GUILayout.Button("引用", GUILayout.Width(100)))
            {
                string[] _TempArray = AssetDatabase.GetDependencies(_AllAssetsPaths[i]);
                string _Temp = "";
                for (int m = 0; m < _TempArray.Length; m++)
                {
                    _Temp += _TempArray[m]+"\n";
                }
                Debug.Log(_Temp);
            }
            GUILayout.FlexibleSpace();
            if (GUILayout.Button("被引用", GUILayout.Width(100)))
            {
                string[] _TempArray = GetUseAssetPaths(_AllAssetsPaths[i]);
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
        EditorGUILayout.BeginHorizontal("HelpBox");
        GUILayout.Label("图标", new GUILayoutOption[] { GUILayout.Width(40), GUILayout.Height(20) });
        GUILayout.Label("名称", GUILayout.Width(300));
        GUILayout.FlexibleSpace();
        GUILayout.Label("", GUILayout.Width(100));
        GUILayout.FlexibleSpace();
        GUILayout.Label("", GUILayout.Width(100));
        GUILayout.FlexibleSpace();
        EditorGUILayout.EndHorizontal();

        for (int i = 0; i < _UnusedAssetsPaths.Count; i++)
        {
            EditorGUILayout.BeginHorizontal("HelpBox");
            GUILayout.Label(AssetDatabase.GetCachedIcon(_UnusedAssetsPaths[i]), new GUILayoutOption[] { GUILayout.Width(40), GUILayout.Height(20) });
            GUILayout.Label(AssetDatabase.LoadMainAssetAtPath(_UnusedAssetsPaths[i]).name, GUILayout.Width(300));
            GUILayout.FlexibleSpace();
            if (GUILayout.Button("选中", GUILayout.Width(100)))
            {
                OnUnusedSelect(_UnusedAssetsPaths[i]);
            }
            GUILayout.FlexibleSpace();
            if (GUILayout.Button("删除", GUILayout.Width(100)))
            {
                OnUnusedDelete(_UnusedAssetsPaths[i]);
            }
            GUILayout.FlexibleSpace();
            EditorGUILayout.EndHorizontal();
        }
    }
    void OnUnusedSelect(string _PathValue)
    {
        Selection.activeObject = AssetDatabase.LoadAssetAtPath<Object>(_PathValue);
    }
    void OnUnusedDelete(string _PathValue)
    {
        if (AssetDatabase.DeleteAsset(_PathValue))
            AssetDatabase.Refresh();
        OnUnusedClick();
    }
    #endregion

    #region 重复资源GUI
    void OnRepeatGUI()
    {
        for (int i = 0; i < _RepeatAssetsPaths.Count; i++)
        {
            //   GUILayout.Label("", GUILayout.Height(10));
            EditorGUILayout.BeginVertical("HelpBox");
            //  List<string> _TempList = _RepeatAssetsPaths[i];
            for (int j = 0; j < _RepeatAssetsPaths[i].Count; j++)
            {
                EditorGUILayout.BeginHorizontal("HelpBox");
                GUILayout.Label(AssetDatabase.GetCachedIcon(_RepeatAssetsPaths[i][j]), new GUILayoutOption[] { GUILayout.Width(40), GUILayout.Height(20) });
                GUILayout.Label(AssetDatabase.LoadMainAssetAtPath(_RepeatAssetsPaths[i][j]).name, GUILayout.Width(300));
                GUILayout.FlexibleSpace();
                if (GUILayout.Button("选中", GUILayout.Width(100)))
                {
                    OnUnusedSelect(_RepeatAssetsPaths[i][j]);
                }
                GUILayout.FlexibleSpace();
                if (GUILayout.Button("合并", GUILayout.Width(100)))
                {
                    OnRepeatMerge(_RepeatAssetsPaths[i][j], _RepeatAssetsPaths[i]);
                    break;
                }
                GUILayout.FlexibleSpace();
                EditorGUILayout.EndHorizontal();
            }
            EditorGUILayout.EndVertical();
        }
       
    }
   
    #region 合并
    void OnRepeatMerge(string _PathValue,List<string> _ListValue)
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
                    string _RealAllText = File.ReadAllText(_AssetsPath+ _OtherPaths[j]).Replace(_OldGUI,_FixedGUID);
                    File.WriteAllText(_AssetsPath + _OtherPaths[j], _RealAllText);
                }
                else
                    _isOtherNative = false;
            }
            //如果没有外部资源引用他 就删除
            if (_isOtherNative)
            {
                AssetDatabase.DeleteAsset(_ListValue[i]);
                _ListValue.RemoveAt(i);
                i--;
            }
        }
        AssetDatabase.Refresh();
        OnRepeatClick();
    }
    #endregion
    #endregion

    #region 获取其他引用Assets的路径
    string[] GetUseAssetPaths(string _AssetPath)
    {
        List<string> _AssetPaths = new List<string>();
        //使用GUID作为判断标准
        string _AssetGUID = AssetDatabase.AssetPathToGUID(_AssetPath);
        //遍历所有Assets
        for (int i = 0; i < _AllAssetsPaths.Count; i++)
        {
            if (_AllAssetsPaths[i] == _AssetPath)
                continue;

            string[] _OtherPaths = AssetDatabase.GetDependencies(_AllAssetsPaths[i]);
            if (_OtherPaths.Length > 1)
            {
                for (int j = 0; j < _OtherPaths.Length; j++)
                {
                    string _OtherGUID = AssetDatabase.AssetPathToGUID(_OtherPaths[j]);
                    if (_AssetGUID == _OtherGUID)
                    {
                        _AssetPaths.Add(_AllAssetsPaths[i]);
                    }
                }
            }
        }
        return _AssetPaths.ToArray();
    }
    #endregion

    #region 获取相同的文件
    string[] GetSameFilePaths(string _PathValue)
    {
        List<string> _AssetPaths = new List<string>();

        string _AssetMD5 = GetFileMD5(_PathValue);
        //遍历所有Assets
        for (int i = 0; i < _AllAssetsPaths.Count; i++)
        {
            if (_AllAssetsPaths[i] == _PathValue)
                continue;
                if (_AssetMD5 == GetFileMD5(_AllAssetsPaths[i]))
                    _AssetPaths.Add(_AllAssetsPaths[i]);

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
        string _Path = Application.dataPath.Replace("Assets", "");
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
        //刷新文件的数据 -- 没找到其他接口
        EditorSettings.serializationMode = SerializationMode.ForceBinary;
        EditorSettings.serializationMode = SerializationMode.ForceText;
    }
    #endregion

}
