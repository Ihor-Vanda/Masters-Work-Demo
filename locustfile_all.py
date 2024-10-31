from datetime import datetime, timedelta
from locust import HttpUser, task, constant_pacing
import random
import threading
from faker import Faker

fake = Faker()

courses = []
students = []
instructors = []
courses_lock = threading.Lock()
students_lock = threading.Lock()
instructors_lock = threading.Lock()

course_data_initialized = False
student_data_initialized = False
instructor_data_initialized = False


class CourseManagerUser(HttpUser):
    host = "http://localhost:5001"
    wait_time = constant_pacing(1)

    def on_start(self):
        global course_data_initialized, courses, students, instructors
        if not course_data_initialized:
            with courses_lock:
                if not course_data_initialized:
                    course_data_initialized = True
                    self.init_courses()
            
    
    def init_courses(self):
        if not courses:
            response = self.client.get("/courses")
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

    # @task
    # def get_courses(self):
    #     response = self.client.get("/courses")
    #     print(f"GET(courses). Status code: {response.status_code}")

            
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
            print(f"GET(courses/id). Status code: {response.status_code}")

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
        with courses_lock:
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

                    # with courses_lock:
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

                # with courses_lock:
                for course in courses:
                    if course["courseId"] == course_id:
                        course["studentIds"] = new_student_ids
                        break

        print(f"PUT(students/id/delete). Status code: {response.status_code}")

    @task
    def add_instructors(self):
        with courses_lock:
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

                    # with courses_lock:
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

                # with courses_lock:
                for course in courses:
                    if course["courseId"] == course_id:
                        course["instructorIds"] = new_ids
                        break
                
        print(f"PUT(instructors/id/delete). Status code: {response.status_code}")
        

class StudentManagerUser(HttpUser):
    host = "http://localhost:5002"
    wait_time = constant_pacing(1)

    def on_start(self):
        global student_data_initialized, students
        if not student_data_initialized:
            with students_lock:
                if not student_data_initialized:
                    self.init_studentIds()
                    student_data_initialized = True
           
    def init_studentIds(self):
        if not students:
            response = self.client.get("/students")
            if response.status_code == 200:
                response_data = response.json()
                for student in response_data:
                    students.append(student["id"])
            
                print("Student IDs initialized:", len(students))
            else:
                print("Failed to fetch students. Status code:", response.status_code)

    def get_random_student(self):
        if students:
            random_index = random.randint(0, len(students) - 1)
            random_student = students[random_index]
            return random_student
        else:
            return None

    @task
    def get_students(self):
        response = self.client.get("/students")
        print(f"GET(students). Status code: {response.status_code}")

    @task
    def create_student(self):
        body = {
            "firstName": fake.first_name(),
            "lastName": fake.last_name(),
            "email": fake.email(),
            "phone": fake.phone_number(),
            "birthDate": (datetime.now() + timedelta(days=random.randint(1, 30))).strftime("%Y-%m-%d"),
        }

        response = self.client.post("/students", json=body)
        print(f"POST(students). Status code: {response.status_code}.")

        if response.status_code != 201 and response.content:
            return
        
        response_data = response.json()
        with students_lock:
            students.append(response_data["id"])

    @task
    def get_student_by_id(self):
        random_student = self.get_random_student()
        if random_student:
            response = self.client.get(f"/students/{random_student}")
            print(f"GET(students/id). Status code: {response.status_code}")

    @task
    def update_student(self):
        body = {
            "firstName": fake.first_name(),
            "lastName": fake.last_name(),
            "email": fake.email(),
            "phone": fake.phone_number(),
            "birthDate": (datetime.now() + timedelta(days=random.randint(1, 30))).strftime("%Y-%m-%d"),
        }

        random_student = self.get_random_student()
        if random_student:
            response = self.client.put(f"/students/{random_student}", json=body)
            print(f"PUT(students). Status code: {response.status_code} {response.text}")

    @task
    def delete_student(self):
        with students_lock:
            random_student = self.get_random_student()
            if random_student:
                response = self.client.delete(f"/students/{random_student}")
                print(f"DELETE(students). Status code: {response.status_code} {response.text}")
                if response.status_code == 204:
                    students.remove(random_student)


class InstructorManagerUser(HttpUser):
    host = "http://localhost:5003"
    wait_time = constant_pacing(1)

    def on_start(self):
        global instructor_data_initialized, instructors
        if not instructor_data_initialized:
            with instructors_lock:
                if not instructor_data_initialized:
                    self.init_instructorIds()
                    instructor_data_initialized = True
        
    def init_instructorIds(self):
        if not instructors:
            response = self.client.get("/instructors")
            if response.status_code == 200:
                response_data = response.json()
                for instructor in response_data:
                    instructors.append(instructor["id"])
            
                print("Instructor IDs initialized:", len(instructors))
            else:
                print("Failed to fetch instructors. Status code:", response.status_code)
            
    def get_random_instructor(self):
        if instructors:
            random_index = random.randint(0, len(instructors) - 1)
            random_instructor = instructors[random_index]
            return random_instructor
        else:
            return None

    @task
    def get_instructor(self):
        response = self.client.get("/instructors")
        print(f"GET(instructors). Status code: {response.status_code}")
        

    @task
    def create_instructor(self):
        body = {
            "firstName": fake.first_name(),
            "lastName": fake.last_name(),
            "email": fake.email(),
            "phone": fake.phone_number(),
            "birthDate": (datetime.now() + timedelta(days=random.randint(1, 30))).strftime("%Y-%m-%d")
        }

        response = self.client.post("/instructors", json=body) 
        print(f"POST(instructors). Status code: {response.status_code}.")

        if response.status_code != 201 and response.content:
            return
        
        response_data = response.json()
        with instructors_lock:
            instructors.append(response_data["id"])

    @task
    def get_instructor_by_id(self):
        random_instructor = self.get_random_instructor()
        response = self.client.get(f"/instructors/{random_instructor}")
        print(f"GET(instructors/id). Status code: {response.status_code}")
            
    @task
    def put_course(self):
        body = {
            "firstName": fake.first_name(),
            "lastName": fake.last_name(),
            "email": fake.email(),
            "phone": fake.phone_number(),
            "birthDate": (datetime.now() + timedelta(days=random.randint(1, 30))).strftime("%Y-%m-%d")
        }
        
        random_instructor = self.get_random_instructor()
        response = self.client.put(f"/instructors/{random_instructor}", json=body)
        
        print(f"PUT(instructors). Status code: {response.status_code} {response.text}")

    @task
    def delete_course(self):
        with instructors_lock:
            random_instructor = self.get_random_instructor()
            
            response = self.client.delete(f"/instructors/{random_instructor}")
            print(f"DELETE(instructors). Status code: {response.status_code} {response.text}")
            if response.status_code != 204: 
                return
            
            instructors.remove(random_instructor)