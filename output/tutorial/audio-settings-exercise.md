# 실습 6 — 오디오 설정

확인일: 2026-11-04

## 1. 오디오 에셋 확인

Assets 내부의 오디오 파일 및 AudioImporter 설정을 검색했으나 임포트된 오디오 클립은 없었다. Project의 `t:AudioClip` 검색은 프로젝트에 저장된 AudioClip 에셋을 찾는 기능이므로, 이 프로젝트의 런타임 생성 클립은 해당 설정 대상이 아니다.

## 2. 현재 오디오 방식

`Assets/Scritps/RunnerAudio.cs`에서 BGM 1개와 효과음 9개를 수식으로 생성한다. `AudioClip.Create(name, data.Length, 1, Rate, false)`의 channels 인자는 1이므로 이미 모노이며 Rate는 22050Hz다.

| 실습 항목 | 현재 결과 |
| --- | --- |
| Force To Mono | 이미 1채널로 생성되므로 별도 변환 불필요 |
| Compression Format Vorbis | AudioImporter 에셋이 없어 적용 대상 없음 |
| Quality 50 | AudioImporter 에셋이 없어 적용 대상 없음 |
| Apply | 변경할 임포터가 없어 수행하지 않음 |
| Play로 소리 확인 | 이번 단계에서 청취 검증은 수행하지 않음 |

앞선 Build Report의 Sounds 0.0 kb는 임포트된 사운드 데이터가 없다는 현재 구조와 일치한다. 런타임 오디오 버퍼의 메모리 사용량이 0이라는 뜻은 아니다.

## 결론

기존 게임의 오디오 생성·재생 방식을 유지했다. 적용 대상을 만들기 위해 오디오 파일을 새로 추가하거나 기존 합성음을 파일로 변환하지 않았다. 추후 실제 BGM/효과음 파일을 도입할 때 Mono, Vorbis, Quality 50을 해당 AudioImporter에 적용하고 Play에서 음질을 확인하면 된다.
