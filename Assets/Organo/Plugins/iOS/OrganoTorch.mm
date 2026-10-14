// Senter (flash) untuk mode kamera biasa Organo di iOS.
#import <AVFoundation/AVFoundation.h>

extern "C" {

bool OrganoTorch_IsAvailable()
{
    AVCaptureDevice *device = [AVCaptureDevice defaultDeviceWithMediaType:AVMediaTypeVideo];
    return device != nil && [device hasTorch] && [device isTorchModeSupported:AVCaptureTorchModeOn];
}

bool OrganoTorch_Set(bool on)
{
    AVCaptureDevice *device = [AVCaptureDevice defaultDeviceWithMediaType:AVMediaTypeVideo];
    if (device == nil || ![device hasTorch]) return false;
    NSError *error = nil;
    if (![device lockForConfiguration:&error]) return false;
    if (on)
        [device setTorchModeOnWithLevel:AVCaptureMaxAvailableTorchLevel error:&error];
    else
        [device setTorchMode:AVCaptureTorchModeOff];
    [device unlockForConfiguration];
    return error == nil;
}

}
