# 중간 프로젝트 — Cloudflare 배포 및 최적화 보고서

프로젝트: Monster Strike Runner / 작성일: 2026-11-04 / Unity 6000.3.23f1

## 빌드 및 배포 구성

- 최종 WebGL 빌드 성공. 출력 경로는 저장소 루트 `web/`이며 `docs/`에도 동일한 결과물을 복사했다.
- Brotli 압축과 Decompression Fallback 활성화. 압축 대상은 `.unityweb`, 로더는 `.loader.js`다.
- `wrangler.jsonc`: `name`은 실제 Cloudflare 프로젝트와 같은 `monsterstrikerunner`, `assets.directory`는 `./web`이다.
- 저장소: https://github.com/LeeGeonMyeong/MonsterStrikeRunner
- 기존 Cloudflare 배포는 사용자 제공 화면에서 성공을 확인했다. 다만 이번 최적화 빌드의 Cloudflare 반영 여부는 아직 확인하지 않았다.
- 제출용 게임 주소: https://monsterstrikerunner.dldl26607.workers.dev/ (사용자 제공).

## 최적화 내용

Build Report 상위 텍스처 중 ForestRoad, RunnerHeroFrames, RunnerMonsterFrames의 WebGL Max Size를 1024에서 512로 줄였다. 원본 PNG는 유지했다. 빌드 스크립트에도 512를 반영했다. Web 대상 Game 뷰의 480×853, 1×에서 배경과 캐릭터의 표시 상태를 확인했다.

오디오는 임포트 파일 없이 코드로 모노·22050Hz 클립을 생성하므로 Vorbis Quality 50을 적용할 AudioImporter는 없었다. 불필요한 오디오 파일 추가는 하지 않았다.

비교의 '전'은 직전 Build Report 실습에서 생성한 실제 빌드다. 그 빌드에도 기존 코드·텍스처 최적화가 포함되어 있으므로 완전한 무최적화 원본으로 해석하지 않는다.

## 파일별 크기 비교

단위는 바이트이며 MB는 1,000,000바이트 기준이다. 파일명이 해시 기반으로 바뀌므로 역할별로 대응시켰다.

| 파일 역할 | 변경 전 docs | 변경 후 web | 감소량 |
| --- | ---: | ---: | ---: |
| WASM (.wasm.unityweb) | 6,241,350 | 6,177,990 | 63,360 |
| 데이터 (.data.unityweb) | 5,724,972 | 4,051,034 | 1,673,938 |
| 프레임워크 (.framework.js.unityweb) | 76,437 | 75,245 | 1,192 |
| 로더 (.loader.js) | 117,894 | 117,894 | 0 |
| hero.png | 23,869 | 25,173 | -1,304 |
| index.html | 4,412 | 4,412 | 0 |
| style.css | 1,277 | 1,277 | 0 |
| .nojekyll | 0 | 0 | 0 |
| 전체 폴더 | **12,190,211** | **10,453,025** | **1,737,186 (14.25%)** |

※ 개별 보조 파일 크기는 실제 측정 결과와 대조하여 아래 측정 JSON을 우선한다. 전체 합계에는 숨김 파일을 포함하며 디스크 할당 크기는 제외한다.

최대 파일은 WASM **6.178 MB**로 25 MB 미만이다. 이번 web과 docs는 동일한 최종 빌드다. 위의 변경 전 docs 값은 교체 전에 저장한 측정값이다.

## Build Report 비교

Unity 로그의 단위와 반올림을 그대로 기록했다. 아래 비압축 보고서 수치는 압축 web 폴더 용량과 측정 기준이 다르다.

| 카테고리 | 전 | 후 |
| --- | --- | --- |
| Textures | 11.6 mb / 88.1% | 7.4 mb / 82.5% |
| Shaders | 855.5 kb / 6.4% | 855.5 kb / 9.3% |
| Other Assets | 358.9 kb / 2.7% | 358.9 kb / 3.9% |
| File headers | 127.3 kb / 0.9% | 127.3 kb / 1.4% |
| Meshes / Animations / Sounds / Levels | 각 0.0 kb / 0.0% | 각 0.0 kb / 0.0% |
| Total User Assets | 13.1 mb | 9.0 mb |
| Complete build size | 27.8 mb | 26.0 mb |

### Used Assets 상위 5개 (후)

| 순위 | 에셋 | 비압축 크기 | 로그 비율 |
| --- | --- | ---: | ---: |
| 1 | Built-in Texture2D: Splash Screen Unity Logo | 2.7 mb | 10.2% |
| 2 | Assets/Resources/Backgrounds/ForestRoad.png | 576.6 kb | 2.2% |
| 3 | Assets/Resources/Characters/RunnerHeroFrames.png | 512.1 kb | 1.9% |
| 4 | Assets/Resources/Characters/RunnerMonsterFrames.png | 342.1 kb | 1.3% |
| 5 | Packages/com.unity.render-pipelines.universal/Textures/SMAA/AreaTex.tga | 262.6 kb | 1.0% |

## 증빙 및 확인 상태

- 변경 전 원문: `../build-report/build-report-raw.txt`
- 변경 후 원문: `../build-report/after-512-raw.txt`
- 변경 전 파일 측정: `../build-report/before-512.json`
- 변경 후 파일 측정: `../build-report/after-512.json`
- Build Report 로그 발췌 이미지: `../build-report/build-report-comparison.png` (로그 원문을 이미지로 정리한 자료이며 Unity 화면 캡처는 아님)
- 실제 휴대폰 첫 로딩 시간 및 한 판 완료: **사용자 확인 필요**. Editor Game 뷰 확인 결과를 휴대폰 테스트로 대체하지 않는다.

게임 접속 QR: [PNG 다운로드](../monster-strike-runner-qr.png).

휴대폰 첫 로딩 시간과 한 판 완료 결과, 최종 빌드의 실제 배포 확인은 추가 기록이 필요하다.
