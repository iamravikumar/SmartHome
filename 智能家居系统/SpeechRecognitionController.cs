﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.ComponentModel;
using System.Speech.Recognition;
using System.Speech.Synthesis;

namespace 智能家居系统
{
    /// <summary>
    /// 实现语音识别控制功能
    /// </summary>
    class SpeechRecognitionController:IDisposable
    {
        /// <summary>
        /// 语音朗读引擎
        /// </summary>
        private SpeechSynthesizer UnitySpeechSynthesizer = null;

        /// <summary>
        /// 语音识别引擎
        /// </summary>
        private SpeechRecognitionEngine UnitySpeechRecognitionEngine = null;

        /// <summary>
        /// 当 SpeechRecognitionEngine 采用与其加载启用的 Grammar 对象匹配的输入的时候引发
        /// </summary>
        public event EventHandler<string> SpeechRecognized;

        /// <summary>
        /// 创建语音朗读引擎
        /// </summary>
        /// <returns></returns>
        public bool CreateSpeechSynthesizer()
        {
            try
            {
                UnitySpeechSynthesizer = new SpeechSynthesizer();
                UnityModule.DebugPrint("创建语音朗读引擎成功");
                UnitySpeechSynthesizer.SpeakStarted += new EventHandler<SpeakStartedEventArgs>(delegate(object s, SpeakStartedEventArgs e ) {
                    UnityModule.DebugPrint("<<<开始语音朗读,暂时停止监听语音指令.");
                    try
                    {
                        if (UnitySpeechRecognitionEngine != null)
                            UnitySpeechRecognitionEngine.RecognizeAsyncStop();
                    }
                    catch (Exception ex)
                    {
                        UnityModule.DebugPrint("暂时停止监听语音指令时遇到错误：{0}", ex.Message);
                    }
                });
                UnitySpeechSynthesizer.SpeakCompleted += new EventHandler<SpeakCompletedEventArgs>(delegate(object s,SpeakCompletedEventArgs e) {
                    UnityModule.DebugPrint(">>>语音朗读结束,重新开始监听语音指令...");
                    try
                    {
                        if (UnitySpeechRecognitionEngine != null)
                            UnitySpeechRecognitionEngine.RecognizeAsync(RecognizeMode.Multiple);
                    }
                    catch (Exception ex) {
                        UnityModule.DebugPrint("重新监听语音指令时遇到错误：{0}",ex.Message);
                    }
                });
                UnityModule.DebugPrint("语音朗读引擎事件绑定成功");
            }
            catch (Exception ex)
            {
                UnityModule.DebugPrint("创建语音朗读引擎遇到错误：{0}",ex.Message);
                return false;
            }
            return true;
        }

        /// <summary>
        /// 创建语音识别引擎
        /// </summary>
        /// <param name="PreferredCulture">优先</param>
        /// <returns></returns>
        public bool CreateSREngine(string PreferredCulture = "zh-CN")
        {
            System.Collections.ObjectModel.ReadOnlyCollection<RecognizerInfo> RecognizerInfoArray;
            try {
                //返回系统中所有语音识别器信息
                RecognizerInfoArray = SpeechRecognitionEngine.InstalledRecognizers();
                UnityModule.DebugPrint("获取系统所有语言识别器信息集合成功");
            
                //语音识别器集合格式小于1，无法创建语音识别器
                if (RecognizerInfoArray.Count < 1)
                {
                    UnityModule.DebugPrint("语音识别器集合包含的识别器个数为 {0} ，无法创建语音识别引擎！",RecognizerInfoArray.Count.ToString());
                    return false;
                }

                //查询匹配的语音识别引擎信息
                RecognizerInfo PreferredRecognizer=null;
                try
                {
                    PreferredRecognizer = RecognizerInfoArray.First<RecognizerInfo>(preferredRecognizer => preferredRecognizer.Culture.ToString() == PreferredCulture);
                    UnityModule.DebugPrint("查询匹配的语音识别引擎信息成功 {0}",PreferredRecognizer.Description);
                }
                catch (Exception exIn)
                {
                    UnityModule.DebugPrint("未查询到匹配的首选语音识别引擎信息：{0}",exIn.Message);
                    PreferredRecognizer = RecognizerInfoArray.First();
                    UnityModule.DebugPrint("将使用系统第一语音识别引擎：{0} ({1}) / {2}", PreferredRecognizer.Name, PreferredRecognizer.Culture.ToString(), PreferredRecognizer.Description);
                }
            
                if (PreferredRecognizer == null)
                {
                    UnityModule.DebugPrint("语音识别器信息为 null，无法创建语音识别引擎");
                    return false;
                }

                UnitySpeechRecognitionEngine = new SpeechRecognitionEngine(PreferredRecognizer);
                UnityModule.DebugPrint("创建语音识别引擎成功！");

                //为语音识别引擎注册事件
                UnitySpeechRecognitionEngine.AudioLevelUpdated += new EventHandler<AudioLevelUpdatedEventArgs>(UnitySREngine_AudioLevelUpdated);
                UnitySpeechRecognitionEngine.SpeechRecognized += new EventHandler<SpeechRecognizedEventArgs>(UnitySREngine_SpeechRecognized);
                UnitySpeechRecognitionEngine.AudioStateChanged += new EventHandler<AudioStateChangedEventArgs>(UnitySREngine_AudioStateChanged);
                UnityModule.DebugPrint("语音识别引擎注册事件成功");

                //使用系统默认音频输入设备
                UnitySpeechRecognitionEngine.SetInputToDefaultAudioDevice();
                UnityModule.DebugPrint("使用默认音频输入设备");

            } catch (Exception ex)
            {
                UnityModule.DebugPrint("启动语音识别引擎时遇到错误：{0}", ex.Message);
                return false;
            }
            return true;
        }

        /// <summary>
        /// 启动语音识别引擎
        /// </summary>
        /// <param name="PreferredCulture">语言区域（默认为 中文/"zh-CN"）</param>
        /// <returns>是否启动成功</returns>
        public bool StartUpSREngine()
        {
            try
            {
                //检测一次，如果语音识别引擎为null，使用默认参数创建一次，并再次检测为null时，结束函数
                if (UnitySpeechRecognitionEngine == null)
                {
                    UnityModule.DebugPrint("语音识别引擎为 null，使用默认参数创建语音识别引擎...");
                    CreateSREngine();
                    if (UnitySpeechRecognitionEngine == null) return false;
                }

                //开始启动执行一个或多个异步语音识别操作
                UnitySpeechRecognitionEngine.RecognizeAsync(RecognizeMode.Multiple);
            }
            catch (Exception ex)
            {
                UnityModule.DebugPrint("启动语音识别引擎时遇到错误：{0}",ex.Message);
                return false;
            }

            return true;
        }


        public void LoadGrammar(Grammar CustomGrammar)
        {
            if (CustomGrammar == null) return;
            if (UnitySpeechRecognitionEngine == null) return;

            try
            {
                UnitySpeechRecognitionEngine.LoadGrammar(CustomGrammar);
            }
            catch (Exception ex)
            {
                UnityModule.DebugPrint("为语音识别引擎导入语法 [{0}] 时遇到错误：{1}",ex.Message);
                //try
                //{
                //    //导入语法出错时，使用系统默认识别语法
                //    UnitySpeechRecognitionEngine.LoadGrammar(new DictationGrammar());
                //}
                //catch (Exception ex1) {
                //    UnityModule.DebugPrint("导入默认语法遇到错误：{0}", ex1.Message);
                //}
            }
        }

        private void UnitySREngine_AudioStateChanged(object sender, AudioStateChangedEventArgs e)
        {
            UnityModule.DebugPrint("语音状态改变：{0}",e.AudioState.ToString());
            //语音状态改变
        }

        /// <summary>
        /// 当 SpeechRecognitionEngine 采用与其加载启用的 Grammar 对象匹配的输入的时候引发
        /// </summary>
        private void UnitySREngine_SpeechRecognized(object sender, SpeechRecognizedEventArgs e)
        {
            //语音识别结束
            UnityModule.DebugPrint(" $ 识别到语音指令：{0}",e.Result.Text );
            //触发事件，转入外部处理
            SpeechRecognized(sender,e.Result.Text);
        }

        private void UnitySREngine_AudioLevelUpdated(object sender, AudioLevelUpdatedEventArgs e)
        {
            //语音音量变化
        }

        /// <summary>
        /// 停止语音识别引擎
        /// </summary>
        public void StopSREngine()
        {
            UnityModule.DebugPrint("停止语音识别引擎...");
            try
            {
                if (UnitySpeechRecognitionEngine == null) return;
                UnitySpeechRecognitionEngine.RecognizeAsyncStop();
            }
            catch (Exception ex)
            {
                UnityModule.DebugPrint("关闭语音识别引擎时出错：{0}"+ ex.Message);
            }
        }

        /// <summary>
        /// 语音播报信息
        /// </summary>
        /// <param name="Message">需要语音播报的信息</param>
        public void VoiceSpeak(string Message)
        {
            try
            {
                //检测一次，如果语朗读别引擎为null，使用默认参数创建一次，并再次检测为null时，结束函数
                if (UnitySpeechSynthesizer == null)
                {
                    UnityModule.DebugPrint("语音朗读引擎为 null，使用默认参数创建语音识别引擎...");
                    CreateSpeechSynthesizer();
                    if (UnitySpeechSynthesizer == null) return;
                }

                UnityModule.DebugPrint("开始语音播报：{0}", Message);
                UnitySpeechSynthesizer.SpeakAsync(Message);
            }
            catch (Exception ex)
            {
                UnityModule.DebugPrint("语音朗读时遇到错误：{0}", ex.Message);
            }
        }

        void IDisposable.Dispose()
        {
            if (UnitySpeechRecognitionEngine != null)
            {
                try
                {
                    UnitySpeechRecognitionEngine.RecognizeAsyncStop();
                    UnitySpeechRecognitionEngine.Dispose();
                    UnitySpeechRecognitionEngine = null;
                }
                catch (Exception ex)
                {
                    UnityModule.DebugPrint("注销语音识别引擎时遇到错误：{0}",ex.Message);
                }
            }
            if (UnitySpeechSynthesizer != null)
            {
                try
                {
                    UnitySpeechSynthesizer.SpeakAsyncCancelAll();
                    UnitySpeechRecognitionEngine.Dispose();
                    UnitySpeechRecognitionEngine = null;
                }
                catch (Exception ex)
                {
                    UnityModule.DebugPrint("注销语音朗读引擎时遇到错误：{0}", ex.Message);
                }
            }
            GC.SuppressFinalize(this);
        }

    }
}
