import argparse
import re
import sys

import argostranslate.translate


SUPPORTED_TARGETS = {"en", "de", "fr", "ja", "zh"}


def translate_text(text: str, to_code: str) -> str:
    if to_code not in SUPPORTED_TARGETS:
        raise ValueError(f"Unsupported target language: {to_code}")
    if to_code == "en":
        translated = argostranslate.translate.translate(text, "es", "en")
    else:
        english_text = argostranslate.translate.translate(text, "es", "en")
        translated = argostranslate.translate.translate(english_text, "en", to_code)

    compact_translation = "".join(translated.split())
    if len(translated) > max(300, len(text) * 12):
        raise ValueError("Installed Argos models returned an implausibly expanded translation.")
    if re.fullmatch(r"(.{2,32})\1+", compact_translation) or re.search(r"(.{2,32})\1{4,}", compact_translation):
        raise ValueError("Installed Argos models returned repetitive output; verify the language packages.")
    return translated


def main() -> int:
    if hasattr(sys.stdout, "reconfigure"):
        sys.stdout.reconfigure(encoding="utf-8")

    parser = argparse.ArgumentParser()
    parser.add_argument("text")
    parser.add_argument("to_code", choices=sorted(SUPPORTED_TARGETS))
    arguments = parser.parse_args()

    try:
        sys.stdout.write(translate_text(arguments.text, arguments.to_code))
    except Exception as error:
        print(f"Offline translation failed: {error}", file=sys.stderr)
        return 1
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
