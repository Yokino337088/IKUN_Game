using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using System;


namespace TangmenFramework
{
    /// <summary>
    /// 音乐音效管理器
    /// </summary>
    public class MusicMgr : BaseManager<MusicMgr>
    {
        //背景音乐播放器
        private AudioSource bkMusic = null;

        //背景音乐音量
        private float bkMusicValue = 0.5f;

        //存储正在播放的音效
        private List<AudioSource> soundList = new List<AudioSource>();
        //音效音量
        private float soundValue = 1f;
        //音效是否正在播放
        private bool soundIsPlay = true;

        //场景加载状态标记
        private bool isSceneChanging = false;

        //音效对象的根节点
        private GameObject soundRoot = null;

        public event Action<int> OnMusicPlaybackCompleted;

        private MusicMgr()
        {
            MonoMgr.Instance.AddFixedUpdateListener(Update);


            SceneMgr.Instance.onSceneLoadStart += OnSceneLoadStart;
            SceneMgr.Instance.onSceneLoadComplete += OnSceneLoadComplete;
        }

        public override void Dispose()
        {
            base.Dispose();
            SceneMgr.Instance.onSceneLoadStart -= OnSceneLoadStart;
            SceneMgr.Instance.onSceneLoadComplete -= OnSceneLoadComplete;
        }

        private void OnSceneLoadStart()
        {
            //场景开始加载时标记状态
            isSceneChanging = true;
        }

        private void OnSceneLoadComplete()
        {
            //场景加载完成后重置状态
            isSceneChanging = false;
        }

        //获取或创建音效对象的根节点
        private GameObject GetSoundRoot()
        {
            if (soundRoot == null)
            {
                soundRoot = GameObject.Find("SoundRoot");
                if (soundRoot == null)
                {
                    soundRoot = new GameObject("SoundRoot");
                    GameObject.DontDestroyOnLoad(soundRoot);
                }
            }
            return soundRoot;
        }

        private void Update()
        {
            //检查音效是否播放完成
            CheckSoundFinished();

            // 检查背景音乐是否播放完成
            CheckBGMusicFinished();
        }

        //检查音效是否播放完成
        private void CheckSoundFinished()
        {
            if (!soundIsPlay)
                return;

            //检查并清理停止的音效，避免内存泄漏
            //为了避免越界，从后往前遍历
            for (int i = soundList.Count - 1; i >= 0; --i)
            {
                if (!soundList[i].isPlaying)
                {
                    //音效播放完毕，清空引用，避免占用音效片段内存
                    soundList[i].clip = null;
                    GOPoolMgr.Instance.PushObj(soundList[i].gameObject);
                    soundList.RemoveAt(i);
                }
            }
        }

        // 检查背景音乐是否播放完成
        private void CheckBGMusicFinished()
        {
            if (musicList != null && musicList.Count > 0 && bkMusic != null && !bkMusic.isPlaying && bkMusic.clip != null)
            {
                // 音乐播放完成，播放下一首
                currentMusicIndex++;
                OnMusicPlaybackCompleted?.Invoke(currentMusicIndex);
                PlayNextMusicInList();
            }
        }


        
        /// <summary>
        /// 播放背景音乐
        /// </summary>
        /// <param name="abName">AB包名字</param>
        /// <param name="name">资源名</param>
        public void PlayBKMusic(string abName, string name)
        {
            //动态创建背景音乐播放器，确保场景切换时也能播放
            //确保背景音乐在场景切换时也能播放
            if (bkMusic == null)
            {
                GameObject obj = new GameObject();
                obj.name = "BKMusic";
                GameObject.DontDestroyOnLoad(obj);
                bkMusic = obj.AddComponent<AudioSource>();
            }

            //根据路径加载背景音乐
            ABResMgr.Instance.LoadResAsync<AudioClip>(abName, name, (clip) =>
            {
                bkMusic.clip = clip;
                bkMusic.loop = true;
                bkMusic.volume = bkMusicValue;
                bkMusic.Play();
            });
        }

        // 当前正在播放的音乐索引
        private int currentMusicIndex = 0;
        // 音乐列表
        private List<string> musicList = null;
        // 当前ab包名称
        private string currentABName = null;
        // 是否循环整个列表
        private bool isLoopList = true;

        /// <summary>
        /// 循环播放背景音乐列表
        /// </summary>
        /// <param name="musicNames">要循环播放的音乐名字列表</param>
        /// <param name="abName">音乐文件所在ab包的名字</param>
        /// <param name="loopList">是否循环整个列表</param>
        public void PlayBKMusicList(List<string> musicNames, string abName = "music", bool loopList = true)
        {
            if (musicNames == null || musicNames.Count == 0)
            {
                LogSystem.Warning("音乐列表为空");
                return;
            }

            // 存储音乐列表、ab包名称和循环标志
            musicList = musicNames;
            currentABName = abName;
            currentMusicIndex = 0;
            isLoopList = loopList;

            // 动态创建背景音乐播放器，确保场景切换时也能播放
            if (bkMusic == null)
            {
                GameObject obj = new GameObject();
                obj.name = "BKMusic";
                GameObject.DontDestroyOnLoad(obj);
                bkMusic = obj.AddComponent<AudioSource>();
            }

            // 开始播放第一首音乐
            PlayNextMusicInList();
        }

        /// <summary>
        /// 从指定歌曲开始循环播放音乐列表（使用已保存的音乐列表和AB包名称）
        /// </summary>
        /// <param name="songName">要开始播放的歌曲名称</param>
        public void PlayBKMusicListFromSong(string songName)
        {
            if (musicList == null || musicList.Count == 0)
            {
                LogSystem.Warning("音乐列表为空，请先调用PlayBKMusicList设置音乐列表");
                return;
            }

            // 在列表中查找指定歌曲的索引
            int targetIndex = -1;
            for (int i = 0; i < musicList.Count; i++)
            {
                if (musicList[i] == songName)
                {
                    targetIndex = i;
                    break;
                }
            }

            if (targetIndex == -1)
            {
                LogSystem.Warning("在音乐列表中未找到歌曲: " + songName);
                return;
            }

            // 设置为从这首歌开始播放
            currentMusicIndex = targetIndex;

            // 动态创建背景音乐播放器，确保场景切换时也能播放
            if (bkMusic == null)
            {
                GameObject obj = new GameObject();
                obj.name = "BKMusic";
                GameObject.DontDestroyOnLoad(obj);
                bkMusic = obj.AddComponent<AudioSource>();
            }

            // 开始播放
            PlayNextMusicInList();
        }

        /// <summary>
        /// 从指定歌曲开始循环播放音乐列表
        /// </summary>
        /// <param name="songName">要开始播放的歌曲名称</param>
        /// <param name="musicNames">音乐列表</param>
        /// <param name="abName">音乐文件所在ab包的名字</param>
        /// <param name="loopList">是否循环整个列表</param>
        public void PlayBKMusicListFromSong(string songName, List<string> musicNames, string abName = "music", bool loopList = true)
        {
            if (musicNames == null || musicNames.Count == 0)
            {
                LogSystem.Warning("音乐列表为空");
                return;
            }

            // 存储音乐列表、ab包名称和循环标志
            musicList = musicNames;
            currentABName = abName;
            isLoopList = loopList;

            // 在列表中查找指定歌曲的索引
            int targetIndex = -1;
            for (int i = 0; i < musicList.Count; i++)
            {
                if (musicList[i] == songName)
                {
                    targetIndex = i;
                    break;
                }
            }

            if (targetIndex == -1)
            {
                LogSystem.Warning("在音乐列表中未找到歌曲: " + songName);
                // 如果找不到指定歌曲，从第一首开始播放
                targetIndex = 0;
            }

            currentMusicIndex = targetIndex;

            // 动态创建背景音乐播放器，确保场景切换时也能播放
            if (bkMusic == null)
            {
                GameObject obj = new GameObject();
                obj.name = "BKMusic";
                GameObject.DontDestroyOnLoad(obj);
                bkMusic = obj.AddComponent<AudioSource>();
            }

            // 开始播放
            PlayNextMusicInList();
        }

        // 播放列表中的下一首音乐
        private void PlayNextMusicInList()
        {
            if (musicList == null || musicList.Count == 0)
                return;

            // 检查是否已经播放完所有音乐
            if (currentMusicIndex >= musicList.Count)
            {
                if (isLoopList)
                {
                    // 如果需要循环，重置索引
                    currentMusicIndex = 0;
                }
                else
                {
                    // 如果不需要循环，停止播放
                    if (bkMusic != null)
                    {
                        bkMusic.Stop();
                    }
                    return;
                }
            }

            string musicName = musicList[currentMusicIndex];

            // 加载并播放音乐
            ABResMgr.Instance.LoadResAsync<AudioClip>(currentABName, musicName, (clip) =>
            {
                if (clip == null)
                {
                    LogSystem.Warning("无法加载音乐: " + musicName);
                    // 尝试播放下一首
                    currentMusicIndex++;
                    PlayNextMusicInList();
                    return;
                }

                bkMusic.clip = clip;
                bkMusic.loop = false; // 关闭单曲循环，以便播放完后切换到下一首
                bkMusic.volume = bkMusicValue;
                bkMusic.Play();

                // 不再使用协程，而是在Update中检查音乐播放完成
            });
        }



        //停止背景音乐
        public void StopBKMusic()
        {
            if (bkMusic == null)
                return;
            bkMusic.Stop();
        }

        //暂停背景音乐
        public void PauseBKMusic()
        {
            if (bkMusic == null)
                return;
            bkMusic.Pause();
        }

        //改变背景音乐音量
        public void ChangeBKMusicValue(float v)
        {
            bkMusicValue = v;
            if (bkMusic == null)
                return;
            bkMusic.volume = bkMusicValue;
        }

        /// <summary>
        /// 播放音效
        /// </summary>
        /// <param name="name">音效名称</param>
        /// <param name="isLoop">是否循环</param>
        /// <param name="isSync">是否同步加载</param>
        /// <param name="callBack">播放完成后的回调</param>
        public void PlaySound(string name, bool isLoop = false, bool isSync = false, Action<AudioSource> callBack = null, float duration = -1f)
        {
            //加载音效资源，异步加载
            ABResMgr.Instance.LoadResAsync<AudioClip>("sound", name, (clip) =>
            {
                //从对象池获取音效对象
                AudioSource source = GOPoolMgr.Instance.GetObj("Sound/soundObj").GetComponent<AudioSource>();
                //在获取新音效之前，将之前使用的播放器停止
                source.Stop();

                source.clip = clip;
                source.loop = isLoop;
                source.volume = soundValue;
                source.Play();
                //存储引用，用于后续检查是否停止
                //由于从对象池获取对象，可能获取到之前使用过的，此时需要
                //检查引用列表中是否存在，不存在则添加，避免重复添加引用
                if (!soundList.Contains(source))
                    soundList.Add(source);
                //如果指定了播放时长，则在指定秒数后自动停止该音效
                if (duration > 0f)
                {
                    TimerMgr.Instance.CreateTimer(false, (int)duration * 1000, () =>
                    {
                        source?.Stop();
                    });
                }
                //回调给外部使用
                callBack?.Invoke(source);
            }, isSync);
        }

        /// <summary>
        /// 从ab包中加载音效并播放
        /// </summary>
        /// <param name="abName">ab包名称</param>
        /// <param name="soundName">音效名称</param>
        /// <param name="isLoop"></param>
        /// <param name="isSync"></param>
        /// <param name="callBack"></param>
        public void PlaySound(string abName, string soundName, bool isLoop = false, bool isSync = false, Action<AudioSource> callBack = null, float duration = -1f)
        {
            //加载音效资源，异步加载
            ABResMgr.Instance.LoadResAsync<AudioClip>(abName, soundName, (clip) =>
            {
                //从对象池获取音效对象
                AudioSource source = GOPoolMgr.Instance.GetObj("Sound/soundObj").GetComponent<AudioSource>();
                //在获取新音效之前，将之前使用的播放器停止
                source.Stop();

                source.clip = clip;
                source.loop = isLoop;
                source.volume = soundValue;
                source.Play();
                //存储引用，用于后续检查是否停止
                //由于从对象池获取对象，可能获取到之前使用过的，此时需要
                //检查引用列表中是否存在，不存在则添加，避免重复添加引用
                if (!soundList.Contains(source))
                    soundList.Add(source);
                //如果指定了播放时长，则在指定秒数后自动停止该音效
                if (duration > 0f)
                {
                    TimerMgr.Instance.CreateTimer(false, (int)duration * 1000, () =>
                    {
                        source?.Stop();
                    });
                }
                //回调给外部使用
                callBack?.Invoke(source);
            }, isSync);
        }

        /// <summary>
        /// 停止指定音效
        /// </summary>
        /// <param name="source">音效播放器</param>
        public void StopSound(AudioSource source)
        {
            if (soundList.Contains(source))
            {
                //停止播放
                source.Stop();
                //从列表中移除
                soundList.Remove(source);
                //清空引用，释放片段，减少内存占用
                source.clip = null;
                //返回对象池
                GOPoolMgr.Instance.PushObj(source.gameObject);
            }
        }

        /// <summary>
        /// 改变音效音量
        /// </summary>
        /// <param name="v"></param>
        public void ChangeSoundValue(float v)
        {
            soundValue = v;
            for (int i = 0; i < soundList.Count; i++)
            {
                soundList[i].volume = v;
            }
        }

        /// <summary>
        /// 控制是否播放所有音效
        /// </summary>
        /// <param name="isPlay">是否继续播放 true为播放 false为暂停</param>
        public void PlayOrPauseSound(bool isPlay)
        {
            if (isPlay)
            {
                soundIsPlay = true;
                for (int i = 0; i < soundList.Count; i++)
                    soundList[i].Play();
            }
            else
            {
                soundIsPlay = false;
                for (int i = 0; i < soundList.Count; i++)
                    soundList[i].Pause();
            }
        }

        /// <summary>
        /// 清空音效引用列表，切换场景时在场景切换之前清空
        /// 重要的事情说三遍：清空
        /// 切换场景时在场景切换之前清空
        /// 切换场景时在场景切换之前清空
        /// 切换场景时在场景切换之前清空
        /// </summary>
        public void ClearSound()
        {
            for (int i = 0; i < soundList.Count; i++)
            {
                soundList[i].Stop();
                soundList[i].clip = null;
                GOPoolMgr.Instance.PushObj(soundList[i].gameObject);
            }
            //清空音效列表
            soundList.Clear();
        }

        /// <summary>
        /// 安全播放音效
        /// </summary>
        /// <param name="name">音效名称</param>
        /// <param name="isLoop">是否循环</param>
        /// <param name="isSync">是否同步加载</param>
        /// <param name="callBack">播放完成后的回调</param>
        public void PlaySoundSafe(string name, bool isLoop = false, bool isSync = false, Action<AudioSource> callBack = null, float duration = -1f)
        {
            //如果正在场景加载中，延迟播放
            if (isSceneChanging && !isSync)
            {
                //延迟到场景加载完成后播放
                TimerMgr.Instance.CreateTimer(true, 100, () =>
                {
                    PlaySound(name, isLoop, isSync, callBack, duration);
                });
                return;
            }

            //加载音效资源，异步加载
            ABResMgr.Instance.LoadResAsync<AudioClip>("sound", name, (clip) =>
            {
                //再次检查场景加载状态
                if (isSceneChanging)
                    return;

                try
                {
                    //从对象池获取音效对象
                    GameObject soundObj = GOPoolMgr.Instance.GetObj("Sound/soundObj");
                    if (soundObj == null)
                    {
                        LogSystem.Warning("无法从对象池获取音效对象");
                        return;
                    }

                    //确保音效对象是根节点的子节点
                    soundObj.transform.SetParent(GetSoundRoot().transform, false);
                    AudioSource source = soundObj.GetComponent<AudioSource>();
                    if (source == null)
                        source = soundObj.AddComponent<AudioSource>();

                    //在获取新音效之前，将之前使用的播放器停止
                    source.Stop();

                    source.clip = clip;
                    source.loop = isLoop;
                    source.volume = soundValue;
                    source.Play();
                    //存储引用，用于后续检查是否停止
                    if (!soundList.Contains(source))
                        soundList.Add(source);
                    //如果指定了播放时长，则在指定秒数后自动停止该音效
                    if (duration > 0f)
                    {
                        TimerMgr.Instance.CreateTimer(false, (int)duration * 1000, () =>
                        {
                            source?.Stop();
                        });
                    }
                    //回调给外部使用
                    callBack?.Invoke(source);
                }
                catch (System.Exception e)
                {
                    LogSystem.Error("播放音效时发生错误: " + e.Message);
                }
            }, isSync);
        }

        /// <summary>
        /// 从ab包中安全加载音效并播放
        /// </summary>
        /// <param name="abName">ab包名称</param>
        /// <param name="soundName">音效名称</param>
        /// <param name="isLoop"></param>
        /// <param name="isSync"></param>
        /// <param name="callBack"></param>
        public void PlaySoundSafe(string abName, string soundName, float duration = -1f, bool isLoop = false, bool isSync = false, Action<AudioSource> callBack = null)
        {
            //如果正在场景加载中，延迟播放
            if (isSceneChanging && !isSync)
            {
                //延迟到场景加载完成后播放
                TimerMgr.Instance.CreateTimer(true, 100, () =>
                {
                    PlaySound(abName, soundName, isLoop, isSync, callBack, duration);
                });
                return;
            }

            //加载音效资源，异步加载
            ABResMgr.Instance.LoadResAsync<AudioClip>(abName, soundName, (clip) =>
            {
                //再次检查场景加载状态
                if (isSceneChanging)
                    return;

                try
                {
                    //从对象池获取音效对象
                    GameObject soundObj = GOPoolMgr.Instance.GetObj("Sound/soundObj");
                    if (soundObj == null)
                    {
                        LogSystem.Warning("无法从对象池获取音效对象");
                        return;
                    }

                    //确保音效对象是根节点的子节点
                    soundObj.transform.SetParent(GetSoundRoot().transform, false);
                    AudioSource source = soundObj.GetComponent<AudioSource>();
                    if (source == null)
                        source = soundObj.AddComponent<AudioSource>();

                    //在获取新音效之前，将之前使用的播放器停止
                    source.Stop();

                    source.clip = clip;
                    source.loop = isLoop;
                    source.volume = soundValue;
                    source.Play();
                    //存储引用，用于后续检查是否停止
                    if (!soundList.Contains(source))
                        soundList.Add(source);
                    //如果指定了播放时长，则在指定秒数后自动停止该音效
                    if (duration > 0f)
                    {
                        TimerMgr.Instance.CreateTimer(false, (int)duration * 1000, () =>
                        {
                            source?.Stop();
                        });
                    }
                    //回调给外部使用
                    callBack?.Invoke(source);
                }
                catch (System.Exception e)
                {
                    LogSystem.Error("播放音效时发生错误: " + e.Message);
                }
            }, isSync);
        }

        /// <summary>
        /// 直接播放AudioClip音效（过场景安全）
        /// </summary>
        /// <param name="clip">音效片段</param>
        /// <param name="isLoop">是否循环</param>
        /// <param name="callBack">播放完成后的回调</param>
        public void PlaySound(AudioClip clip, bool isLoop = false, Action<AudioSource> callBack = null)
        {
            //如果正在场景加载中，延迟播放
            if (isSceneChanging)
            {
                //延迟到场景加载完成后播放
                TimerMgr.Instance.CreateTimer(true, 100, () =>
                {
                    PlaySound(clip, isLoop, callBack);
                });
                return;
            }

            try
            {
                //从对象池获取音效对象
                GameObject soundObj = GOPoolMgr.Instance.GetObj("Sound/soundObj");
                if (soundObj == null)
                {
                    LogSystem.Warning("无法从对象池获取音效对象");
                    return;
                }

                //确保音效对象是根节点的子节点
                soundObj.transform.SetParent(GetSoundRoot().transform, false);
                AudioSource source = soundObj.GetComponent<AudioSource>();
                if (source == null)
                    source = soundObj.AddComponent<AudioSource>();

                //在获取新音效之前，将之前使用的播放器停止
                source.Stop();

                source.clip = clip;
                source.loop = isLoop;
                source.volume = soundValue;
                source.Play();
                //存储引用，用于后续检查是否停止
                if (!soundList.Contains(source))
                    soundList.Add(source);
                //回调给外部使用
                callBack?.Invoke(source);
            }
            catch (System.Exception e)
            {
                LogSystem.Error("播放音效时发生错误: " + e.Message);
            }
        }


    }
}