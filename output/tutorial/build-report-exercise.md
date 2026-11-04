# 실습 4 — Build Report 읽기

기록일: 2026-11-04 / Unity 6000.3.23f1 / WebGL

## 1. web 빌드

Unity의 `Runner > Build Web for Compression Exercise` 메뉴로 실제 새 빌드를 실행했다. Editor.log의 `RUNNER_WEB_BUILD_PASS: C:\Git\MonsterStrikeRunner\web`로 성공을 확인했다. Brotli 압축과 Decompression Fallback을 사용했다.

기존 프로젝트에 적용된 최적화 설정을 유지했으므로, 아래 값은 '최적화 전' 원본이 아닌 현재 상태의 기준값이다. 이번 결과는 로컬 파일이며 GitHub 푸시나 Cloudflare 재배포는 실행하지 않았다.

## 2–3. Editor Log에서 Build Report 확보

로그 위치: `C:\Users\user\AppData\Local\Unity\Editor\Editor.log`.

로그에서 `Build Report`를 검색해 마지막 보고서를 추출했다. 다음 빌드나 Editor 재시작으로 기록이 바뀌는 경우에 대비해 원문을 `output/build-report/build-report-raw.txt`에 보관했다. Unity UI에서는 Console 우상단 메뉴의 Open Editor Log를 통해 같은 내용을 확인할 수 있다.

## 4. 카테고리 비율

원문의 단위와 반올림을 그대로 옮겼다. 비율의 기준은 Unity가 명시한 사용자 생성 에셋의 비압축 용량이다.

| 카테고리 | 비압축 크기(로그 표기) | 비율 |
| --- | ---: | ---: |
| Textures | 11.6 mb | 88.1% |
| Meshes | 0.0 kb | 0.0% |
| Animations | 0.0 kb | 0.0% |
| Sounds | 0.0 kb | 0.0% |
| Shaders | 855.5 kb | 6.4% |
| Other Assets | 358.9 kb | 2.7% |
| Levels | 0.0 kb | 0.0% |
| File headers | 127.3 kb | 0.9% |
| Total User Assets | 13.1 mb | 100.0% |

`Complete build size`: **27.8 mb**(로그 표기). 이 값은 디스크의 Brotli 압축 web 폴더 합계와 측정 기준이 다르다.

### Used Assets 상위 5개

원문 순서와 비율을 그대로 기록했다. 이 목록의 비율은 위 카테고리 표의 비율과 분모가 같다고 가정하지 않는다.

| 순위 | 에셋 | 비압축 크기 | 로그 비율 |
| --- | --- | ---: | ---: |
| 1 | Built-in Texture2D: Splash Screen Unity Logo | 2.7 mb | 9.6% |
| 2 | Assets/Resources/Backgrounds/ForestRoad.png | 2.3 mb | 8.1% |
| 3 | Assets/Resources/Characters/RunnerHeroFrames.png | 2.0 mb | 7.2% |
| 4 | Assets/Resources/Characters/RunnerMonsterFrames.png | 1.3 mb | 4.8% |
| 5 | Packages/com.unity.render-pipelines.universal/Textures/SMAA/AreaTex.tga | 262.6 kb | 0.9% |

텍스처가 88.1%이므로 이후 용량 개선을 검토할 때 배경과 캐릭터 프레임의 해상도·포맷을 우선 확인할 수 있다. 이번 실습에서 추가 최적화는 수행하지 않았다.

## 5. web 폴더 크기

숨김 파일 포함 8개 파일의 논리적 크기 합계: **12,190,211바이트 = 12.190 MB = 약 11.625 MiB**. 파일 시스템의 디스크 할당 크기는 별개다.

| 빌드 파일 | 바이트 |
| --- | ---: |
| e8e5b546b337b0bce525c225e23148c9.wasm.unityweb | 6,241,350 |
| 21e5a85f64d814d6eaf34bb3e610989f.data.unityweb | 5,724,972 |
| 4896a40c28d562081cb32d351a2a004b.framework.js.unityweb | 76,437 |
| d264b190c1749f61616ce86b3291424b.loader.js | 117,894 |

최대 파일은 약 **6.241 MB**로 25 MB 미만이다. 이전 빌드의 해시 파일 4개는 처음에 별도 보관했다가 사용자 요청에 따라 삭제했다. `docs/`도 이번 `web/` 빌드로 교체했으며, 전체 8개 파일의 SHA-256 일치를 확인했다.
