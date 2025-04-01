using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using MI_GUI_WinUI.Services.Interfaces;

namespace MI_GUI_WinUI.Services
{
    public class DialogService : IDialogService
    {
        private readonly SemaphoreSlim _dialogSemaphore = new SemaphoreSlim(1, 1);
        private readonly Queue<(ContentDialog Dialog, TaskCompletionSource<ContentDialogResult> Result)> _pendingDialogs = 
            new Queue<(ContentDialog, TaskCompletionSource<ContentDialogResult>)>();
        
        public async Task<ContentDialogResult> ShowDialogAsync(ContentDialog dialog)
        {
            var tcs = new TaskCompletionSource<ContentDialogResult>();
            
            // Queue the dialog request
            bool shouldShow = false;
            
            lock (_pendingDialogs)
            {
                if (_dialogSemaphore.CurrentCount > 0)
                {
                    shouldShow = true;
                }
                else
                {
                    _pendingDialogs.Enqueue((dialog, tcs));
                }
            }
            
            if (shouldShow)
            {
                await ShowDialogInternalAsync(dialog, tcs);
            }
            
            return await tcs.Task;
        }
        
        private async Task ShowDialogInternalAsync(ContentDialog dialog, TaskCompletionSource<ContentDialogResult> tcs)
        {
            try
            {
                await _dialogSemaphore.WaitAsync();
                
                ContentDialogResult result = ContentDialogResult.None;
                
                // Show dialog on UI thread
                await dialog.DispatcherQueue.EnqueueAsync(async () =>
                {
                    try
                    {
                        result = await dialog.ShowAsync();
                    }
                    catch (Exception ex)
                    {
                        tcs.TrySetException(ex);
                    }
                });
                
                tcs.TrySetResult(result);
            }
            catch (Exception ex)
            {
                tcs.TrySetException(ex);
            }
            finally
            {
                _dialogSemaphore.Release();
                
                // Process next dialog if any
                ProcessNextDialogAsync();
            }
        }
        
        private void ProcessNextDialogAsync()
        {
            ContentDialog nextDialog = null;
            TaskCompletionSource<ContentDialogResult> nextTcs = null;
            
            lock (_pendingDialogs)
            {
                if (_pendingDialogs.Count > 0)
                {
                    (nextDialog, nextTcs) = _pendingDialogs.Dequeue();
                }
            }
            
            if (nextDialog != null)
            {
                // Fire and forget - the Task is tracked via the TCS
                _ = ShowDialogInternalAsync(nextDialog, nextTcs);
            }
        }
        
        public async Task ShowErrorAsync(Window window, string title, string message)
        {
            var dialog = new ContentDialog
            {
                Title = title,
                Content = message,
                PrimaryButtonText = "OK",
                XamlRoot = window.Content.XamlRoot
            };
            
            await ShowDialogAsync(dialog);
        }
    }
    
    // Helper extension for DispatcherQueue
    public static class DispatcherQueueExtensions
    {
        public static Task EnqueueAsync(this Microsoft.UI.Dispatching.DispatcherQueue dispatcher, Func<Task> function)
        {
            var tcs = new TaskCompletionSource<object>();
            
            dispatcher.TryEnqueue(async () =>
            {
                try
                {
                    await function();
                    tcs.TrySetResult(null);
                }
                catch (Exception ex)
                {
                    tcs.TrySetException(ex);
                }
            });
            
            return tcs.Task;
        }
    }
}