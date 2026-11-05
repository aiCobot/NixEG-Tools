#!/usr/bin/env python3
"""
EVision.Quality 注册码生成器
算法：MD5("JKVISION:N:机器码:反馈码:N:JKVISION")
其中 N = 天数

通过三组样本验证：
机器码=ARPMNIZARMJHIDEB
  反馈码=NUVMNECA, 天数=90 → 27B4CD1B-8256BE6D-193E2FB3-455522DC
  反馈码=EZAECAXS, 天数=15 → 0C8F9CD8-9606956B-19416286-8E58AC62
  反馈码=MWRSPNIJ, 天数=30 → 7AD8CB37-39007F48-7AB6379A-2C3C7764
"""

import hashlib
import sys

def generate_license(machine_code: str, feedback_code: str, days: int) -> str:
    """
    根据机器码、反馈码和天数生成注册码
    """
    # 构建输入字符串
    input_str = f"JKVISION:{days}:{machine_code}:{feedback_code}:{days}:JKVISION"
    
    # 计算 MD5（Latin-1 编码）
    data = input_str.encode('latin-1')
    md5_hash = hashlib.md5(data).hexdigest().upper()
    
    # 格式化为 XXXX-XXXX-XXXX-XXXX-XXXX-XXXX-XXXX-XXXX
    license_code = f"{md5_hash[0:8]}-{md5_hash[8:16]}-{md5_hash[16:24]}-{md5_hash[24:32]}"
    
    return license_code


def verify_license(machine_code: str, feedback_code: str, days: int, license_code: str) -> bool:
    """
    验证注册码是否正确
    """
    expected = generate_license(machine_code, feedback_code, days)
    return expected.upper() == license_code.upper()


def main():
    # 样本验证
    print("=== 样本验证 ===")
    samples = [
        ("ARPMNIZARMJHIDEB", "NUVMNECA", 90, "27B4CD1B-8256BE6D-193E2FB3-455522DC"),
        ("ARPMNIZARMJHIDEB", "EZAECAXS", 15, "0C8F9CD8-9606956B-19416286-8E58AC62"),
        ("ARPMNIZARMJHIDEB", "MWRSPNIJ", 30, "7AD8CB37-39007F48-7AB6379A-2C3C7764"),
    ]
    
    all_pass = True
    for machine, fb, days, expected in samples:
        result = generate_license(machine, fb, days)
        status = "✓" if result == expected else "✗"
        print(f"  {status} 机器码={machine}, 反馈码={fb}, 天数={days}")
        print(f"     生成: {result}")
        print(f"     期望: {expected}")
        if result != expected:
            all_pass = False
    
    print(f"\n样本验证：{'全部通过' if all_pass else '有失败'}")
    
    # 命令行模式
    if len(sys.argv) >= 4:
        machine = sys.argv[1]
        fb = sys.argv[2]
        days = int(sys.argv[3])
        code = generate_license(machine, fb, days)
        print(f"\n注册码: {code}")
    elif len(sys.argv) == 5 and sys.argv[1] == '--verify':
        machine = sys.argv[2]
        fb = sys.argv[3]
        days = int(sys.argv[4])
        code = sys.argv[5] if len(sys.argv) > 5 else input("输入注册码: ")
        ok = verify_license(machine, fb, days, code)
        print(f"验证结果: {'✓ 有效' if ok else '✗ 无效'}")


if __name__ == '__main__':
    main()
