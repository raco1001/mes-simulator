#!/usr/bin/env python3
import json
from pathlib import Path

from jsonschema import Draft7Validator, RefResolver


ROOT = Path(__file__).resolve().parent
EXAMPLES = ROOT / "examples"


def load_json(path: Path):
    with path.open("r", encoding="utf-8") as f:
        return json.load(f)


def build_store():
    common = load_json(ROOT / "ontology-common-defs.json")
    prop = load_json(ROOT / "property-definition.json")
    obj = load_json(ROOT / "object-type-schema.json")
    link = load_json(ROOT / "link-type-schema.json")
    return common, prop, obj, link, {
        common["$id"]: common,
        prop["$id"]: prop,
        obj["$id"]: obj,
        link["$id"]: link,
    }


def validate_file(validator, path: Path):
    data = load_json(path)
    errors = sorted(validator.iter_errors(data), key=lambda e: list(e.path))
    return errors


def main():
    common, prop, object_schema, link, store = build_store()
    resolver = RefResolver.from_schema(object_schema, store=store)
    object_validator = Draft7Validator(object_schema, resolver=resolver)

    valid_files = ["freezer.json", "battery.json", "conveyor.json"]
    invalid_files = ["invalid-missing-required.json", "invalid-enum.json"]

    print("== Valid fixtures ==")
    valid_ok = True
    for name in valid_files:
        errors = validate_file(object_validator, EXAMPLES / name)
        if errors:
            valid_ok = False
            print(f"[FAIL] {name}")
            for err in errors:
                path = ".".join(str(p) for p in err.path) or "<root>"
                print(f"  - {path}: {err.message}")
        else:
            print(f"[PASS] {name}")

    print("\n== Negative fixtures ==")
    invalid_ok = True
    for name in invalid_files:
        errors = validate_file(object_validator, EXAMPLES / "invalid" / name)
        if not errors:
            invalid_ok = False
            print(f"[FAIL] {name} expected invalid, but passed")
        else:
            print(f"[PASS] {name} rejected as expected")

    if valid_ok and invalid_ok:
        print("\nPhase 10 example validation passed.")
        return 0

    print("\nPhase 10 example validation failed.")
    return 1


if __name__ == "__main__":
    raise SystemExit(main())
