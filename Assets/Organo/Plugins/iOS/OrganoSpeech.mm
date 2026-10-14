// Narasi organ Organo di iOS: AVSpeechSynthesizer dengan suara Bahasa Indonesia (id-ID).
// Dipilih suara id-ID dengan kualitas tertinggi yang terpasang (Premium > Enhanced > bawaan) supaya terdengar lebih alami.
#import <AVFoundation/AVFoundation.h>

static AVSpeechSynthesizer *s_Synth = nil;

static AVSpeechSynthesisVoice *OrganoBestIndonesianVoice()
{
    AVSpeechSynthesisVoice *best = nil;
    for (AVSpeechSynthesisVoice *v in [AVSpeechSynthesisVoice speechVoices])
    {
        if (![v.language hasPrefix:@"id"]) continue;
        if (best == nil || v.quality > best.quality) best = v;
    }
    return best != nil ? best : [AVSpeechSynthesisVoice voiceWithLanguage:@"id-ID"];
}

extern "C" {

bool OrganoSpeech_HasIndonesian()
{
    return OrganoBestIndonesianVoice() != nil;
}

void OrganoSpeech_Speak(const char *text, float rate, float pitch)
{
    if (text == NULL) return;
    if (s_Synth == nil) s_Synth = [[AVSpeechSynthesizer alloc] init];
    [s_Synth stopSpeakingAtBoundary:AVSpeechBoundaryImmediate];
    AVSpeechUtterance *u = [AVSpeechUtterance speechUtteranceWithString:[NSString stringWithUTF8String:text]];
    AVSpeechSynthesisVoice *voice = OrganoBestIndonesianVoice();
    if (voice != nil) u.voice = voice;
    // AVSpeechUtteranceDefaultSpeechRate = 0.5; rate 1 = normal
    float r = AVSpeechUtteranceDefaultSpeechRate * rate;
    if (r < AVSpeechUtteranceMinimumSpeechRate) r = AVSpeechUtteranceMinimumSpeechRate;
    if (r > AVSpeechUtteranceMaximumSpeechRate) r = AVSpeechUtteranceMaximumSpeechRate;
    u.rate = r;
    u.pitchMultiplier = pitch;
    [s_Synth speakUtterance:u];
}

void OrganoSpeech_Stop()
{
    if (s_Synth != nil) [s_Synth stopSpeakingAtBoundary:AVSpeechBoundaryImmediate];
}

bool OrganoSpeech_IsSpeaking()
{
    return s_Synth != nil && [s_Synth isSpeaking];
}

}
