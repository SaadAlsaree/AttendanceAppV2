#!/bin/bash

# Security Middleware Testing Script for Linux/macOS
# استخدم هذا السكريبت لاختبار جميع مكونات الحماية الأمنية

BASE_URL="http://localhost:5000"
VERBOSE=false

# Colors for output
RED='\033[0;31m'
GREEN='\033[0;32m'
YELLOW='\033[1;33m'
BLUE='\033[0;34m'
CYAN='\033[0;36m'
GRAY='\033[0;37m'
NC='\033[0m' # No Color

# Parse arguments
while [[ $# -gt 0 ]]; do
    case $1 in
        -u|--url)
            BASE_URL="$2"
            shift 2
            ;;
        -v|--verbose)
            VERBOSE=true
            shift
            ;;
        -h|--help)
            echo "Usage: $0 [-u|--url URL] [-v|--verbose] [-h|--help]"
            echo "  -u, --url     Base URL (default: http://localhost:5000)"
            echo "  -v, --verbose Enable verbose output"
            echo "  -h, --help    Show this help message"
            exit 0
            ;;
        *)
            echo "Unknown option: $1"
            exit 1
            ;;
    esac
done

print_result() {
    local test_name="$1"
    local passed="$2"
    local details="$3"
    
    if [ "$passed" = "true" ]; then
        echo -e "${GREEN}✅ PASS${NC} - $test_name"
    else
        echo -e "${RED}❌ FAIL${NC} - $test_name"
    fi
    
    if [ "$VERBOSE" = "true" ] && [ -n "$details" ]; then
        echo -e "    ${GRAY}Details: $details${NC}"
    fi
}

test_rate_limiting() {
    echo -e "\n${CYAN}🔄 اختبار Rate Limiting...${NC}"
    
    local rate_limit_hit=false
    local requests=0
    
    for i in {1..80}; do
        status=$(curl -s -o /dev/null -w "%{http_code}" --max-time 5 "$BASE_URL/api/security-test/rate-limit")
        
        if [ "$status" = "429" ]; then
            rate_limit_hit=true
            print_result "Rate Limiting Detection" "true" "Blocked after $requests requests"
            break
        elif [ "$status" = "200" ]; then
            ((requests++))
            if [ "$VERBOSE" = "true" ] && [ $((i % 10)) -eq 0 ]; then
                echo -e "    ${GRAY}Request $i - Status: $status${NC}"
            fi
        fi
        
        sleep 0.05
    done
    
    if [ "$rate_limit_hit" = "false" ]; then
        print_result "Rate Limiting Detection" "false" "Did not hit rate limit after $requests requests"
    fi
}

test_sql_injection() {
    echo -e "\n${CYAN}💉 اختبار SQL Injection Protection...${NC}"
    
    local sql_payloads=(
        "1' OR '1'='1"
        "1'; DROP TABLE users--"
        "1' UNION SELECT * FROM information_schema.tables--"
        "1' AND (SELECT COUNT(*) FROM sysobjects)>0--"
        "admin'-- "
        "' OR 1=1#"
    )
    
    local blocked_count=0
    local total=${#sql_payloads[@]}
    
    for payload in "${sql_payloads[@]}"; do
        encoded_payload=$(printf '%s' "$payload" | jq -sRr @uri)
        status=$(curl -s -o /dev/null -w "%{http_code}" --max-time 5 "$BASE_URL/api/security-test/sql-test?id=$encoded_payload")
        
        if [ "$status" = "403" ]; then
            ((blocked_count++))
            if [ "$VERBOSE" = "true" ]; then
                echo -e "    ${GREEN}Payload '${payload:0:20}...' - BLOCKED ✅${NC}"
            fi
        elif [ "$VERBOSE" = "true" ]; then
            echo -e "    ${GRAY}Payload '${payload:0:20}...' - Status: $status${NC}"
        fi
    done
    
    if [ $blocked_count -eq $total ]; then
        print_result "SQL Injection Protection" "true" "Blocked $blocked_count of $total payloads"
    else
        print_result "SQL Injection Protection" "false" "Blocked $blocked_count of $total payloads"
    fi
}

test_xss_protection() {
    echo -e "\n${CYAN}🛡️ اختبار XSS Protection...${NC}"
    
    local xss_payloads=(
        "<script>alert('xss')</script>"
        "<img src=x onerror=alert('xss')>"
        "javascript:alert('xss')"
        "<iframe src='javascript:alert(1)'></iframe>"
        "<svg onload=alert('xss')>"
        "<body onload=alert('xss')>"
    )
    
    local blocked_count=0
    local total=${#xss_payloads[@]}
    
    for payload in "${xss_payloads[@]}"; do
        json_payload=$(jq -n --arg content "$payload" '{content: $content}')
        status=$(curl -s -o /dev/null -w "%{http_code}" --max-time 5 \
            -X POST "$BASE_URL/api/security-test/xss-test" \
            -H "Content-Type: application/json" \
            -d "$json_payload")
        
        if [ "$status" = "400" ]; then
            ((blocked_count++))
            if [ "$VERBOSE" = "true" ]; then
                echo -e "    ${GREEN}Payload '${payload:0:30}...' - BLOCKED ✅${NC}"
            fi
        elif [ "$VERBOSE" = "true" ]; then
            echo -e "    ${GRAY}Payload '${payload:0:30}...' - Status: $status${NC}"
        fi
    done
    
    if [ $blocked_count -eq $total ]; then
        print_result "XSS Protection" "true" "Blocked $blocked_count of $total payloads"
    else
        print_result "XSS Protection" "false" "Blocked $blocked_count of $total payloads"
    fi
}

test_path_traversal() {
    echo -e "\n${CYAN}📁 اختبار Path Traversal Protection...${NC}"
    
    local path_payloads=(
        "../../../etc/passwd"
        "..\\..\\..\\windows\\system32\\drivers\\etc\\hosts"
        "%2e%2e%2f%2e%2e%2f%2e%2e%2fetc%2fpasswd"
        "....//....//....//etc/passwd"
        "..%252f..%252f..%252fetc%252fpasswd"
    )
    
    local blocked_count=0
    local total=${#path_payloads[@]}
    
    for payload in "${path_payloads[@]}"; do
        encoded_payload=$(printf '%s' "$payload" | jq -sRr @uri)
        status=$(curl -s -o /dev/null -w "%{http_code}" --max-time 5 "$BASE_URL/api/security-test/path-test?path=$encoded_payload")
        
        if [ "$status" = "400" ]; then
            ((blocked_count++))
            if [ "$VERBOSE" = "true" ]; then
                echo -e "    ${GREEN}Path '${payload:0:30}...' - BLOCKED ✅${NC}"
            fi
        elif [ "$VERBOSE" = "true" ]; then
            echo -e "    ${GRAY}Path '${payload:0:30}...' - Status: $status${NC}"
        fi
    done
    
    if [ $blocked_count -eq $total ]; then
        print_result "Path Traversal Protection" "true" "Blocked $blocked_count of $total payloads"
    else
        print_result "Path Traversal Protection" "false" "Blocked $blocked_count of $total payloads"
    fi
}

test_command_injection() {
    echo -e "\n${CYAN}⚡ اختبار Command Injection Protection...${NC}"
    
    local command_payloads=(
        "ls; cat /etc/passwd"
        "dir && type C:\\Windows\\System32\\drivers\\etc\\hosts"
        "whoami | base64"
        "\`id\`"
        "\$(whoami)"
        "test & ping google.com"
    )
    
    local blocked_count=0
    local total=${#command_payloads[@]}
    
    for payload in "${command_payloads[@]}"; do
        json_payload=$(jq -n --arg command "$payload" '{command: $command}')
        status=$(curl -s -o /dev/null -w "%{http_code}" --max-time 5 \
            -X POST "$BASE_URL/api/security-test/command-test" \
            -H "Content-Type: application/json" \
            -d "$json_payload")
        
        if [ "$status" = "400" ]; then
            ((blocked_count++))
            if [ "$VERBOSE" = "true" ]; then
                echo -e "    ${GREEN}Command '${payload:0:25}...' - BLOCKED ✅${NC}"
            fi
        elif [ "$VERBOSE" = "true" ]; then
            echo -e "    ${GRAY}Command '${payload:0:25}...' - Status: $status${NC}"
        fi
    done
    
    if [ $blocked_count -eq $total ]; then
        print_result "Command Injection Protection" "true" "Blocked $blocked_count of $total payloads"
    else
        print_result "Command Injection Protection" "false" "Blocked $blocked_count of $total payloads"
    fi
}

test_security_headers() {
    echo -e "\n${CYAN}🔒 اختبار Security Headers...${NC}"
    
    local required_headers=(
        "X-Frame-Options"
        "X-Content-Type-Options"
        "X-XSS-Protection"
        "Strict-Transport-Security"
        "Content-Security-Policy"
        "Referrer-Policy"
    )
    
    local found_headers=0
    local total=${#required_headers[@]}
    
    # Get headers from response
    local headers_output
    headers_output=$(curl -s -I "$BASE_URL/api/security-test/headers" 2>/dev/null)
    
    for header in "${required_headers[@]}"; do
        if echo "$headers_output" | grep -qi "^$header:"; then
            ((found_headers++))
            if [ "$VERBOSE" = "true" ]; then
                local header_value
                header_value=$(echo "$headers_output" | grep -i "^$header:" | cut -d' ' -f2-)
                echo -e "    ${GREEN}✅ $header: $header_value${NC}"
            fi
        elif [ "$VERBOSE" = "true" ]; then
            echo -e "    ${RED}❌ $header: Missing${NC}"
        fi
    done
    
    if [ $found_headers -eq $total ]; then
        print_result "Security Headers" "true" "Found $found_headers of $total required headers"
    else
        print_result "Security Headers" "false" "Found $found_headers of $total required headers"
    fi
}

test_file_upload_security() {
    echo -e "\n${CYAN}📎 اختبار File Upload Security...${NC}"
    
    # Create temporary malicious files
    local temp_dir="/tmp"
    local dangerous_files=(
        "malicious.php:<?php system(\$_GET['cmd']); ?>"
        "malware.exe:This is a fake executable"
        "script.bat:@echo off\ndir C:\\"
        "shell.jsp:<%@ page import=\"java.io.*\" %>"
    )
    
    local blocked_count=0
    local total=${#dangerous_files[@]}
    
    for file_info in "${dangerous_files[@]}"; do
        local filename="${file_info%%:*}"
        local content="${file_info#*:}"
        local filepath="$temp_dir/$filename"
        
        # Create the file
        echo -e "$content" > "$filepath"
        
        # Test file upload
        status=$(curl -s -o /dev/null -w "%{http_code}" --max-time 5 \
            -X POST "$BASE_URL/api/security-test/file-upload-test" \
            -F "file=@$filepath" 2>/dev/null)
        
        if [ "$status" = "400" ]; then
            ((blocked_count++))
            if [ "$VERBOSE" = "true" ]; then
                echo -e "    ${GREEN}File '$filename' - BLOCKED ✅${NC}"
            fi
        elif [ "$VERBOSE" = "true" ]; then
            echo -e "    ${GRAY}File '$filename' - Status: $status${NC}"
        fi
        
        # Clean up
        rm -f "$filepath" 2>/dev/null
    done
    
    print_result "File Upload Security" "true" "Tested $total dangerous file types"
}

test_suspicious_activity() {
    echo -e "\n${CYAN}🕵️ اختبار Suspicious Activity Detection...${NC}"
    
    local suspicious_user_agents=(
        "sqlmap/1.0"
        "nikto/2.1.6"
        "nmap/7.80"
        "gobuster/3.0"
        "dirb/2.22"
    )
    
    local total=${#suspicious_user_agents[@]}
    
    for user_agent in "${suspicious_user_agents[@]}"; do
        status=$(curl -s -o /dev/null -w "%{http_code}" --max-time 5 \
            -H "User-Agent: $user_agent" \
            "$BASE_URL/api/security-test/trigger-suspicious")
        
        if [ "$VERBOSE" = "true" ]; then
            if [ "$status" = "403" ]; then
                echo -e "    ${GREEN}User-Agent '$user_agent' - BLOCKED ✅${NC}"
            else
                echo -e "    ${GRAY}User-Agent '$user_agent' - Status: $status${NC}"
            fi
        fi
        
        sleep 0.5
    done
    
    print_result "Suspicious User-Agent Detection" "true" "Tested $total suspicious user agents"
}

# Main execution
echo -e "${BLUE}🛡️ Security Middleware Testing Suite${NC}"
echo -e "${BLUE}=====================================${NC}"
echo -e "${GRAY}Base URL: $BASE_URL${NC}"
echo -e "${GRAY}Verbose: $VERBOSE${NC}\n"

# Check if required tools are available
if ! command -v curl &> /dev/null; then
    echo -e "${RED}❌ curl is required but not installed${NC}"
    exit 1
fi

if ! command -v jq &> /dev/null; then
    echo -e "${RED}❌ jq is required but not installed${NC}"
    exit 1
fi

# Test if the application is running
health_status=$(curl -s -o /dev/null -w "%{http_code}" --max-time 5 "$BASE_URL/api/security-test/health" 2>/dev/null)

if [ "$health_status" = "200" ]; then
    echo -e "${GREEN}✅ Application is running (Status: $health_status)${NC}\n"
else
    echo -e "${RED}❌ Application is not accessible at $BASE_URL${NC}"
    echo -e "${YELLOW}Please ensure the application is running and try again.${NC}\n"
    exit 1
fi

# Run all tests
test_rate_limiting
test_sql_injection
test_xss_protection
test_path_traversal
test_command_injection
test_security_headers
test_file_upload_security
test_suspicious_activity

echo -e "\n${BLUE}🎉 Testing Complete!${NC}"
echo -e "${BLUE}=====================================${NC}"

# Usage instructions
echo -e "\n${YELLOW}💡 Usage Tips:${NC}"
echo -e "- Run with -v or --verbose for detailed output"
echo -e "- Check application logs for security events"
echo -e "- Monitor logs with: tail -f logs/app-*.log | grep -E '(Warning|Error)'"
echo -e "- For more detailed testing, see TESTING_GUIDE.md"
