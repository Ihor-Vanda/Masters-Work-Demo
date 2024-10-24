import threading
from locust import HttpUser, task, constant_pacing
import random
from datetime import datetime, timedelta

courses = []
students = []
instructors = []
courses_lock = threading.Lock()

class CourseManagerUser(HttpUser):
    wait_time = constant_pacing(1)

    def on_start(self):
        global courses, students, instructors
        self.init_courses()
        self.init_studentIds()
        self.init_instructorIds()
    
    def init_courses(self):
        response = self.client.get("http://localhost:5001/courses")
        if response.status_code == 200:
            response_data = response.json()
            for course in response_data:
                courses.append(
                {
                    "courseId": course["id"],
                    "studentIds": course["students"],
                    "instructorIds": course["instructors"],
                    "maxStudents": course["maxStudents"]
                })
        
            print("Course IDs initialized:", len(courses))
        else:
            print("Failed to fetch courses. Status code:", response.status_code)
            
    def init_studentIds(self):
        response = self.client.get("http://localhost:5002/students")
        if response.status_code == 200:
            response_data = response.json()
            for student in response_data:
                students.append(student["id"])
           
            print("Student IDs initialized:", len(students))
        else:
            print("Failed to fetch students. Status code:", response.status_code)
            
    def init_instructorIds(self):
        response = self.client.get("http://localhost:5003/instructors")
        if response.status_code == 200:
            response_data = response.json()
            for instructor in response_data:
                instructors.append(instructor["id"])
        
            print("Instructor IDs initialized:", len(instructors))
        else:
            print("Failed to fetch instructors. Status code:", response.status_code)
        
    def generate_course_body(self, course_id=None):
        course_code = f"COURSE-{random.randint(1000, 9999)}"
        title = f"Course Title {random.randint(1, 100)}"
        description = "This is a description of the course."
        language = "English"
        status = "Active"
        start_date = (datetime.now() + timedelta(days=random.randint(1, 30))).strftime("%Y-%m-%d")
        end_date = (datetime.now() + timedelta(days=random.randint(31, 60))).strftime("%Y-%m-%d")

        if course_id:
            current_students = 0
            max_students = 0
            
            for course in courses:
                if course["courseId"] == course_id:
                    current_students = len(course["studentIds"])
                    max_students = course["maxStudents"]
                    break
            max_students = max(current_students, max_students + random.randint(0, max_students))
                
        else:
            max_students = random.randint(1, 50)

        body = {
            "courseCode": course_code,
            "title": title,
            "description": description,
            "language": language,
            "status": status,
            "startDate": start_date,
            "endDate": end_date, 
            "maxStudents": max_students
        }
        return body

    @task
    def get_courses(self):
        response = self.client.get("/courses")
        print(f"GET(courses). Status code: {response.status_code}")

            
    @task
    def create_course(self):
        body = self.generate_course_body()

        response = self.client.post("/courses", json=body) 

        if response.status_code != 201 and response.content:
            return

        print(f"POST(courses). Status code: {response.status_code}")

        response_data = response.json()
        
        with courses_lock:
            courses.append({
                "courseId": response_data["id"],
                "studentIds": [],
                "instructorIds": [],
                "maxStudents": body["maxStudents"]
            })
            
    @task
    def get_course_by_id(self):
        random_course = random.choice(courses)
        if random_course:
            response = self.client.get(f"/courses/{random_course['courseId']}")
            print(f"GET(courses/id). Status code: {response.status_code} {response.text}")

        else:
            print("No course available to fetch.")
            
    @task
    def put_course(self):
        random_course = random.choice(courses)
        if random_course:
            body = self.generate_course_body(course_id=random_course['courseId'])
            response = self.client.put(f"/courses/{random_course['courseId']}", json=body)
            print(f"PUT(courses/id). Status code: {response.status_code}")

        else:
            print("No course available to update.")
        
    @task
    def delete_course(self):
        with courses_lock:
            random_course = random.choice(courses)
            if random_course:
                response = self.client.delete(f"/courses/{random_course['courseId']}")
                if response.status_code == 204:
                    courses.remove(random_course)
        
                print(f"DELETE(courses/id). Status code: {response.status_code} {response.text}")
            else:
                print("No course available to delete.")
                

    @task
    def add_students(self):
        random_course = random.choice(courses)
        
        if random_course:
            std_ids = random_course["studentIds"]
            max_students = random_course["maxStudents"]
            current_student_count = len(std_ids)

            if current_student_count >= max_students:
                return

            course_id = random_course["courseId"]
            body = []

            if isinstance(students, list) and students:
                remaining_slots = max_students - current_student_count
                count = min(random.randint(1, remaining_slots), remaining_slots)

                selected = []
                for _ in range(count):
                    id = random.choice(students)
                    
                    if id and id not in std_ids and id not in selected:
                        selected.append(id)
                        
                body = selected

            response = self.client.put(f"/students/{course_id}/add", json=body)

            if response.status_code == 200:
                response_data = response.json()
                new_student_ids = response_data["students"]

                with courses_lock:
                    for course in courses:
                        if course["courseId"] == course_id:
                            course["studentIds"] = new_student_ids
                            break

            print(f"PUT(students/id/add). Status code: {response.status_code}")

                
    @task
    def delete_students(self):
        available_course = None
        with courses_lock:
            for course in courses:
                if course["studentIds"]:
                    available_course = course
                    break
        
            if not available_course:
                return

            std_ids = available_course["studentIds"]
            course_id = available_course["courseId"]

            body = []
            
            if std_ids:
                count = random.randint(1, len(std_ids))
                
                selected = []
                for _ in range(count):
                    id = random.choice(std_ids)
                    
                    if id and id in std_ids and id not in selected:
                        selected.append(id)
                        
                body = selected

            response = self.client.put(f"/students/{course_id}/delete", json=body)

            if response.status_code == 200:
                response_data = response.json()
                new_student_ids = response_data["students"]

                with courses_lock:
                    for course in courses:
                        if course["courseId"] == course_id:
                            course["studentIds"] = new_student_ids
                            break

            print(f"PUT(students/id/delete). Status code: {response.status_code}")

    @task
    def add_instructors(self):
        random_course = random.choice(courses)
        
        if random_course:
            instr_ids = random_course["instructorIds"]
            current_count = len(instr_ids)

            if current_count >= 10:
                return

            course_id = random_course["courseId"]
            body = []

            if isinstance(instructors, list) and instructors:
                remaining_slots = 10 - current_count
                count = min(random.randint(1, remaining_slots), remaining_slots)

                selected = []
                for _ in range(count):
                    id = random.choice(instructors)
                    
                    if id and id not in instr_ids and id not in selected:
                        selected.append(id)
                        
                body = selected

            response = self.client.put(f"/instructors/{course_id}/add", json=body)

            if response.status_code == 200:
                response_data = response.json()
                new_student_ids = response_data["instructors"]

                with courses_lock:
                    for course in courses:
                        if course["courseId"] == course_id:
                            course["instructorIds"] = new_student_ids
                            break

            print(f"PUT(instructors/id/add). Status code: {response.status_code}")

    @task
    def delete_instructors(self):
        available_course = None
        with courses_lock:
            for course in courses:
                if course["instructorIds"]:
                    available_course = course
                    break
            
            if not available_course:
                return

            instr_ids = available_course["instructorIds"]
            course_id = available_course["courseId"]

            body = []
            
            if instr_ids:
                count = random.randint(1, len(instr_ids))
                
                selected = []
                for _ in range(count):
                    id = random.choice(instr_ids)
                    
                    if id and id in instr_ids and id not in selected:
                        selected.append(id)
                        
                body = selected

            response = self.client.put(f"/instructors/{course_id}/delete", json=body)

            if response.status_code == 200:
                response_data = response.json()
                new_ids = response_data["instructors"]

                with courses_lock:
                    for course in courses:
                        if course["courseId"] == course_id:
                            course["instructorIds"] = new_ids
                            break
                
            print(f"PUT(instructors/id/delete). Status code: {response.status_code}")

            